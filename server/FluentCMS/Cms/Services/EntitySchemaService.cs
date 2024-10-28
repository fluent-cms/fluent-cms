using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntitySchemaService(ISchemaService schemaService, IDefinitionExecutor definitionExecutor)
    : IEntitySchemaService
{
    public async Task<Result<AttributeVector>> ResolveAttributeVector(LoadedEntity entity, string fieldName)
    {
        var fields = fieldName.Split(".");
        var lastField = fields.Last();

        fields = fields[..^1];
        string prefix = string.Join(AttributeVectorConstants.Separator, fields);

        var attributes = new List<LoadedAttribute>();
        foreach (var field in fields)
        {
            var attr = entity.Attributes.FindOneAttribute(field);
            if (attr is null)
            {
                return Result.Fail($"Fail to resolve filter: no field {fieldName} ");
            }

            var res = await LoadOneRelated(entity, attr, default);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            attr = res.Value;
            switch (attr.Type)
            {
                case DisplayType.Crosstable:
                    entity = attr.Crosstable!.TargetEntity;
                    break;
                case DisplayType.Lookup:
                    entity = attr.Lookup!;
                    break;
                default:
                    return Result.Fail($"Can not resolve {fieldName}, {attr.Field} is not a composite type");
            }

            attributes.Add(attr);
        }

        var last = entity.Attributes.FindOneAttribute(lastField);
        if (last is null)
        {
            return Result.Fail($"Fail to resolve filter: no field ${fieldName} ");
        }
        return new AttributeVector(fieldName, prefix, [..attributes], last);
    }

    public async Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken cancellationToken = default)
    {
        var (_, isFailed, entity, errors) = await GetEntity(name, cancellationToken);
        if (isFailed)
        {
            return Result.Fail(errors);
        }

        var ret = await LoadAllRelated(entity.ToLoadedEntity(definitionExecutor.Cast), false, cancellationToken);
        return ret;
    }

    private async Task<Result<Entity>> GetEntity(string name, CancellationToken cancellationToken = default)
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
            Attributes: [..cols.Select(AttributeHelper.ToAttribute)]
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
            foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.Crosstable))
            {
                await CreateCrosstable(entity.ToLoadedEntity(definitionExecutor.Cast),
                    attribute.ToLoaded(entity.TableName), cancellationToken);
            }
        }

        Result EnsureTableNotExist()
        {
            var creatingNewEntity = dto.Id == 0;
            var tableExists = cols.Length > 0;
            return creatingNewEntity && tableExists
                ? Result.Fail($"Fail to add new entity, the table {entity.TableName} aleady exists")
                : Result.Ok();
        }
    }

    private async Task<Result<LoadedAttribute>> LoadLookup(LoadedAttribute attribute,
        CancellationToken cancellationToken)
    {
        var (_, isFailed, value, _) = await GetEntity(attribute.GetLookupTarget(), cancellationToken);
        if (isFailed)
        {
            return Result.Fail(
                $"not find entity by name {attribute.GetLookupTarget()} for lookup {attribute.GetFullName()}");
        }

        return attribute with { Lookup = value.ToLoadedEntity(definitionExecutor.Cast) };
    }

    private async Task<Result<LoadedAttribute>> LoadCrosstable(LoadedEntity entity, LoadedAttribute attribute,
        CancellationToken cancellationToken)
    {
        var (_, isFailed, targetEntity, _) =
            await GetEntity(attribute.GetCrosstableTarget(), cancellationToken);
        if (isFailed)
        {
            return Result.Fail(
                $"not find entity by name {attribute.GetCrosstableTarget()} for crosstable {attribute.GetFullName()}");
        }

        var loadedTarget =
            await LoadAllRelated(targetEntity.ToLoadedEntity(definitionExecutor.Cast), true, cancellationToken);
        if (loadedTarget.IsFailed)
        {
            return Result.Fail(loadedTarget.Errors);
        }

        return attribute with { Crosstable = CrosstableHelper.Crosstable(entity, loadedTarget.Value, attribute) };
    }

    public async Task<Result<LoadedAttribute>> LoadOneRelated(LoadedEntity entity, LoadedAttribute attribute,
        CancellationToken cancellationToken)
    {
        return attribute.Type switch
        {
            DisplayType.Crosstable => await LoadCrosstable(entity, attribute, cancellationToken),
            DisplayType.Lookup => await LoadLookup(attribute, cancellationToken),
            _ => attribute
        };
    }

    //omitCrosstable: omit  circular reference
    private async Task<Result<LoadedEntity>> LoadAllRelated(LoadedEntity entity, bool omitCrosstable,
        CancellationToken cancellationToken)
    {
        var lst = new List<LoadedAttribute>();
        foreach (var attribute in entity.Attributes)
        {
            if (attribute.Type == DisplayType.Lookup || attribute.Type == DisplayType.Crosstable && !omitCrosstable)
            {
                var (isSuccess, _, value, errors) =
                    await LoadOneRelated(entity, attribute, cancellationToken);
                if (isSuccess)
                {
                    lst.Add(value);
                }
                else
                {
                    return Result.Fail(errors);
                }
            }
            else
            {
                lst.Add(attribute);
            }
        }

        return entity with { Attributes = [..lst] };
    }

    private async Task CreateCrosstable(LoadedEntity entity, LoadedAttribute attribute,
        CancellationToken cancellationToken)
    {
        var targetEntity = CheckResult(await GetLoadedEntity(attribute.GetCrosstableTarget(), cancellationToken));
        var crossTable = CrosstableHelper.Crosstable(entity, targetEntity,attribute);
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
        NotNull(await GetEntity(attribute.GetLookupTarget(), cancellationToken))
            .ValOrThrow($"not find entity by name {attribute.GetLookupTarget()}");
    }

    private async Task VerifyEntity(Entity entity, CancellationToken cancellationToken)
    {
        CheckResult(TitleAttributeExists());
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.Lookup))
        {
            await CheckLookup(attribute, cancellationToken);
        }

        return;

        Result TitleAttributeExists()
        {
            var attribute = entity.Attributes.FirstOrDefault(x => x.Field == entity.TitleAttribute);
            return attribute is null
                ? Result.Fail($"`{entity.TitleAttribute}` was not in attributes list")
                : Result.Ok();
        }
    }

    public async Task<Schema> AddOrUpdate(Entity entity, CancellationToken cancellationToken)
    {
        var find = await schemaService.GetByNameDefault(entity.Name, SchemaType.Entity, cancellationToken);
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