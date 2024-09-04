using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntitySchemaService( ISchemaService schemaService, IDefinitionExecutor definitionExecutor): IEntitySchemaService
{
    public async Task<Result<Entity>> GetByNameDefault(string name, bool loadRelated,
        CancellationToken cancellationToken = default)
    {

        var item = NotNull(await schemaService.GetByNameDefault(name, SchemaType.Entity, cancellationToken))
            .ValOrThrow($"can not find entity {name}");

        var entity = item.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"Not find entity {name}");
        }

        entity.Init();
        if (loadRelated)
        {
            var result = await LoadRelated(entity, cancellationToken);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }

        return entity;
    }

    public Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    
    public async Task<Schema> SaveTableDefine(Schema dto, CancellationToken cancellationToken = default)
    {
        var entity = NotNull(dto.Settings.Entity).ValOrThrow("invalid payload");
        entity.Init();
        entity.EnsureDefaultAttribute();
        var cols = await definitionExecutor.GetColumnDefinitions(entity.TableName, cancellationToken);
        await VerifyEntity(dto, cols, entity, cancellationToken);
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.crosstable))
        {
            await CreateCrosstable(entity, attribute, cancellationToken);
        }

        if (cols.Length > 0) //if table exists, alter table add columns
        {
            var columnDefinitions = entity.AddedColumnDefinitions(cols);
            if (columnDefinitions.Length > 0)
            {
                await definitionExecutor.AlterTableAddColumns(entity.TableName, columnDefinitions, cancellationToken);
            }
        }
        else
        {
            entity.EnsureDeleted();
            await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions(), cancellationToken);
            //no need to expose deleted field to frontend 
            entity.RemoveDeleted();
        }

        await schemaService.EnsureEntityInTopMenuBar(entity, cancellationToken);
        return await schemaService.Save(dto, cancellationToken);
    }    

    public async Task<Schema> AddOrSaveSimpleEntity(string entityName, string field, string? lookup, string? crossTable,
        CancellationToken cancellationToken)
    {
        var entity = new Entity
        {
            Name = entityName,
            TableName = entityName,
            Title = entityName,
            DefaultPageSize = 10,
            PrimaryKey = "id",
            TitleAttribute = field,
            Attributes =
            [
                new Attribute
                {
                    Field = field,
                    Header = field,
                    InList = true,
                    InDetail = true,
                    DataType = DataType.String
                }
            ]
        };
        if (!string.IsNullOrWhiteSpace(lookup))
        {
            entity.Attributes = entity.Attributes.Append(new Attribute
            {
                Field = lookup,
                Options = lookup,
                Header = lookup,
                InList = true,
                InDetail = true,
                DataType = DataType.Int,
                Type = DisplayType.lookup,
            }).ToArray();

        }

        if (!string.IsNullOrWhiteSpace(crossTable))
        {
            entity.Attributes = entity.Attributes.Append(new Attribute
            {
                Field = crossTable,
                Options = crossTable,
                Header = crossTable,
                DataType = DataType.Na,
                Type = DisplayType.crosstable,
                InDetail = true,
            }).ToArray();
        }

        return await AddOrSaveEntity(entity, cancellationToken);
    }


    private async Task<Result> LoadRelated(Entity entity, CancellationToken cancellationToken)
    {
        var res = await LoadLookups(entity, cancellationToken);
        if (res.IsFailed) return res;
        return await LoadCrosstables(entity, cancellationToken);
    }


    public async Task<Result> LoadLookup(Attribute attribute, CancellationToken cancellationToken)
    {
        var lookupEntityName = attribute.GetLookupEntityName();
        if (lookupEntityName.IsFailed)
        {
            return Result.Fail(lookupEntityName.Errors);
        }

        var lookup = await GetByNameDefault(lookupEntityName.Value, false, cancellationToken);
        if (lookup.IsFailed)
        {
            return Result.Fail($"not find entity by name {lookupEntityName} for lookup {attribute.FullName()}");
        }

        attribute.Lookup = lookup.Value;
        return Result.Ok();
    }

    public async Task<Result> LoadCrosstable(Entity sourceEntity, Attribute attribute,
        CancellationToken cancellationToken)
    {
        var targetEntityName = attribute.GetCrossEntityName();
        if (targetEntityName.IsFailed)
        {
            return Result.Fail(targetEntityName.Errors);
        }

        var targetEntity = await GetByNameDefault(targetEntityName.Value, false, cancellationToken);
        if (targetEntity.IsFailed)
        {
            return Result.Fail($"not find entity by name {targetEntityName} for crosstable {attribute.FullName()}");
        }

        attribute.Crosstable = new Crosstable(sourceEntity, targetEntity.Value);
        return await LoadLookups(targetEntity.Value, cancellationToken);

    }

    private async Task<Result> LoadLookups(Entity entity, CancellationToken cancellationToken)
    {
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.lookup))
        {
            var res = await LoadLookup(attribute, cancellationToken);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return Result.Ok();
    }

    private async Task<Result> LoadCrosstables(Entity entity, CancellationToken cancellationToken)
    {
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.crosstable))
        {
            var res = await LoadCrosstable(entity, attribute, cancellationToken);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return Result.Ok();
    }

    private async Task CreateCrosstable(Entity entity, Attribute attribute, CancellationToken cancellationToken)
    {
        var entityName = CheckResult(attribute.GetCrossEntityName());
        var targetEntity = CheckResult(await GetByNameDefault(entityName, false, cancellationToken));
        await LoadRelated(targetEntity, cancellationToken);
        var crossTable = new Crosstable(entity, targetEntity);
        var columns =
            await definitionExecutor.GetColumnDefinitions(crossTable.CrossEntity.TableName, cancellationToken);
        if (columns.Length == 0)
        {
            await definitionExecutor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions(),
                cancellationToken);
        }
    }

    private async Task CheckLookup(Attribute attribute, CancellationToken cancellationToken)
    {
        True(attribute.DataType == DataType.Int).ThrowNotTrue("lookup datatype should be int");
        var entityName = CheckResult(attribute.GetLookupEntityName());
        NotNull(await GetByNameDefault(entityName, false, cancellationToken))
            .ValOrThrow($"not find entity by name {entityName}");
    }
    
    private async Task VerifyEntity(Schema dto, ColumnDefinition[] cols, Entity entity, CancellationToken cancellationToken)
    {
        CheckResult(await schemaService.NameNotTakenByOther(dto, cancellationToken));
        CheckResult(TableExistsWhenCreatingNewEntity());
        CheckResult(TitleAttributeExists());
        
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.lookup))
        {
            await CheckLookup(attribute,cancellationToken);
        }

        return;

        Result TitleAttributeExists()
        {
            var attribute = entity.DisplayTitleAttribute();
            return attribute is null ? Result.Fail($"`{entity.TitleAttribute}` was not in attributes list") : Result.Ok();
        }
        
        Result TableExistsWhenCreatingNewEntity()
        {
            var creatingNewEntity = dto.Id == 0;
            var tableExists = cols.Length > 0;
            return creatingNewEntity && tableExists ? Result.Fail($"Fail to add new entity, the table {entity.TableName} aleady exists") : Result.Ok();
        } 
    }

    private async Task<Schema> AddOrSaveEntity(Entity entity, CancellationToken cancellationToken)
    {
        var find = await schemaService.GetByNameDefault(entity.Name, SchemaType.Entity , cancellationToken);
        var schema = new Schema
        {
            Name = entity.Name,
            Type = SchemaType.Entity,
            Settings = new Settings
            {
                Entity = entity
            }
        };

        if (find is not null)
        {
            schema.Id = find.Id;
        }
        return await SaveTableDefine(schema, cancellationToken);
    }
}