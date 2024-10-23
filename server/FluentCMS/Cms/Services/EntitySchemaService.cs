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
    public async Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken cancellationToken = default)
    {
        var (_, isFailed, entity, errors) = await GetValidEntity(name, cancellationToken);
        if (isFailed)
        {
            return Result.Fail(errors);
        }
        return await LoadRelated(entity,  false, cancellationToken);
    }

    private async Task<Result<ValidEntity>> GetValidEntity(string name, CancellationToken cancellationToken = default)
    {
        var (_,isFailed,entity,errors) = await GetEntity(name,cancellationToken);
        if (isFailed)
        {
            return Result.Fail(errors);
        }

        return entity.ToValid();
    }

    private async Task<Result<Entity>> GetEntity(string name,CancellationToken cancellationToken = default)
    {
        var item = await schemaService.GetByNameDefault(name, SchemaType.Entity, cancellationToken);
        if (item is null)
        {
            return Result.Fail($"Not find entity {name}");
        }
        var entity = item.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"Not find entity {name}");
        }
        return entity;
    }

    public async Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken)
    {
        var cols = await definitionExecutor.GetColumnDefinitions(tableName, cancellationToken);
        return new Entity
        (
            Attributes : [..cols.Select(AttributeHelper.ToAttribute)]
        );
    }

    
    public async Task<Schema> SaveTableDefine(Schema dto, CancellationToken cancellationToken = default)
    {
        CheckResult(await schemaService.NameNotTakenByOther(dto, cancellationToken));
        var entity = NotNull(dto.Settings.Entity).ValOrThrow("invalid payload").WithDefaultAttr();
        var cols = await definitionExecutor.GetColumnDefinitions(entity.TableName, cancellationToken);
        CheckResult(EnsureTableNotExist());
        await VerifyEntity(entity, cancellationToken);
        await SaveSchema(); //need  to save first because it will call trigger
        await CreateCrosstables();
        await SaveMainTable();
        await schemaService.EnsureEntityInTopMenuBar(entity, cancellationToken);
        return dto;

        async Task SaveSchema()
        {
            dto = dto with { Settings = new Settings(entity) };
            dto = await schemaService.Save(dto, cancellationToken);
            entity = dto.Settings.Entity!;
        }

        async Task SaveMainTable()
        {
            if (cols.Length > 0) //if table exists, alter table add columns
            {
                var columnDefinitions = entity.AddedColumnDefinitions(cols);
                if (columnDefinitions.Length > 0)
                {
                    await definitionExecutor.AlterTableAddColumns(entity.TableName, columnDefinitions,
                        cancellationToken);
                }
            }
            else
            {
                await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions().EnsureDeleted(),
                    cancellationToken);
            }
        }

        async Task CreateCrosstables()
        {
            var validEntity = entity.ToValid();
            foreach (var attribute in validEntity.Attributes.GetAttributesByType(DisplayType.Crosstable))
            {
                await CreateCrosstable(validEntity.ToLoadedEntity([]), attribute, cancellationToken);
            }
        }

        Result EnsureTableNotExist()
        {
            var creatingNewEntity = dto.Id == 0;
            var tableExists = cols.Length > 0;
            return creatingNewEntity && tableExists ? Result.Fail($"Fail to add new entity, the table {entity.TableName} aleady exists") : Result.Ok();
        } 
    }    

    private async Task<Result<LoadedEntity>> LoadRelated(ValidEntity entity, bool omitCrosstable, CancellationToken cancellationToken)
    {
        var lst = new List<LoadedAttribute>();
        foreach (var validAttribute in entity.Attributes)
        {
            LoadedEntity? lookup = default;
            Crosstable? crosstable = default;
            if (validAttribute.Type == DisplayType.Lookup )
            {
                var (_,isFailed,value,_) = await GetValidEntity(validAttribute.GetLookupTarget(), cancellationToken);
                if (isFailed)
                {
                    return Result.Fail($"not find entity by name {validAttribute.GetLookupTarget()} for lookup {validAttribute.Fullname}");
                }
                lookup = value.ToLoadedEntity([]);
            }
            
            if (validAttribute.Type == DisplayType.Crosstable && !omitCrosstable)
            {
                //can not call GetLoaded Directly, it will cause infinite loop;
                var (_,isFailed,targetEntity, _) = await GetValidEntity(validAttribute.GetCrosstableTarget(), cancellationToken);
                if (isFailed)
                {
                    return Result.Fail($"not find entity by name {validAttribute.GetCrosstableTarget()} for crosstable {validAttribute.Fullname}");
                }
                var loadedTarget = await LoadRelated(targetEntity,  true, cancellationToken);
                if (loadedTarget.IsFailed)
                {
                    return Result.Fail(loadedTarget.Errors);
                    
                }
                crosstable = CrosstableHelper.Crosstable(entity.ToLoadedEntity([]), loadedTarget.Value);
            }
            lst.Add(validAttribute.ToLoaded(lookup,crosstable));
        }

        return entity.ToLoadedEntity(lst.ToArray());
    }

    private async Task CreateCrosstable(LoadedEntity entity, ValidAttribute attribute, CancellationToken cancellationToken)
    {
        var targetEntity = CheckResult(await GetLoadedEntity(attribute.GetCrosstableTarget(),cancellationToken));
        var crossTable = CrosstableHelper.Crosstable(entity, targetEntity);
        var columns =
            await definitionExecutor.GetColumnDefinitions(crossTable.CrossEntity.TableName, cancellationToken);
        if (columns.Length == 0)
        {
            await definitionExecutor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions(),
                cancellationToken);
        }
    }

    private async Task CheckLookup(ValidAttribute attribute, CancellationToken cancellationToken)
    {
        True(attribute.DataType == DataType.Int).ThrowNotTrue("lookup datatype should be int");
        NotNull(await GetEntity(attribute.GetLookupTarget(), cancellationToken))
            .ValOrThrow($"not find entity by name {attribute.GetLookupTarget()}");
    }
    
    private async Task VerifyEntity(Entity entity, CancellationToken cancellationToken)
    {
        CheckResult(TitleAttributeExists());
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.Lookup))
        {
            await CheckLookup(attribute.ToValid(entity.TableName),cancellationToken);
        }
        return;
        Result TitleAttributeExists()
        {
            var attribute = entity.Attributes.FirstOrDefault(x =>x.Field == entity.TitleAttribute);
            return attribute is null ? Result.Fail($"`{entity.TitleAttribute}` was not in attributes list") : Result.Ok();
        }
    }

    public async Task<Schema> AddOrUpdate(Entity entity, CancellationToken cancellationToken)
    {
        var find = await schemaService.GetByNameDefault(entity.Name, SchemaType.Entity , cancellationToken);
        var schema = new Schema
        (
            Id: find?.Id ?? 0,
            Name: entity.Name,
            Type: SchemaType.Entity,
            Settings: new Settings
            (
                Entity: entity
            ),
            CreatedBy: ""
        );
        return await SaveTableDefine(schema, cancellationToken);
    }
}