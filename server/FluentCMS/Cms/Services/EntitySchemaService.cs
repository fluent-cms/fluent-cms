using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntitySchemaService(ISchemaService schemaSvc, IDefinitionExecutor executor)
    : IEntitySchemaService, IAttributeResolver
{
    public bool GetAttrVal(Attribute attribute, string v, out object? result) => executor.CastToDatabaseDataType(v, attribute.DataType, out result);

    public async Task<Result<AttributeVector>> GetAttrVector(LoadedEntity entity, string fieldName)
    {

        var fields = fieldName.Split(".");
        var prefix = string.Join(AttributeVectorConstants.Separator, fields[..^1]);
        var attributes = new List<LoadedAttribute>();
        LoadedAttribute? attr = null;
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            attr = entity.Attributes.FindOneAttr(field);
            if (attr is null)
            {
                return Result.Fail($"Fail to attribute vector: can not find {field} in {entity.Name} ");
            }

            if (i == fields.Length - 1) break;

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

        return new AttributeVector(fieldName, prefix, [..attributes], attr!);

    }

    public async Task<LoadedAttribute?> FindAttribute(string name, string attr, CancellationToken token)
    {
        var entity = CheckResult(await GetLoadedEntity(name, token));
        return entity.Attributes.FindOneAttr(attr);
    }


    public async Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken token = default)
    {
        var (_, isFailed, entity, errors) = await GetEntity(name, token);
        if (isFailed)
        {
            return Result.Fail(errors);
        }

        var ret = await LoadAllRelated(entity.ToLoadedEntity(), false, token);
        return ret;
    }

    private async Task<Result<Entity>> GetEntity(string name, CancellationToken token = default)
    {
        var item = await schemaSvc.GetByNameDefault(name, SchemaType.Entity, token);
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

    public async Task<Entity?> GetTableDefine(string name, CancellationToken token)
    {
        var cols = await executor.GetColumnDefinitions(name, token);
        return new Entity
        (
            Attributes: [..cols.Select(AttributeHelper.ToAttribute)]
        );
    }


    public async Task<Schema> SaveTableDefine(Schema dto, CancellationToken token = default)
    {
        CheckResult(await schemaSvc.NameNotTakenByOther(dto, token));
        var entity = NotNull(dto.Settings.Entity).ValOrThrow("invalid payload").WithDefaultAttr();
        var cols = await executor.GetColumnDefinitions(entity.TableName, token);
        CheckResult(EnsureTableNotExist());
        await VerifyEntity(entity, token);
        await SaveSchema(); //need  to save first because it will call trigger
        await CreateCrosstables();
        await SaveMainTable();
        await schemaSvc.EnsureEntityInTopMenuBar(entity, token);
        return dto;

        async Task SaveSchema()
        {
            dto = dto with { Settings = new Settings(entity) };
            dto = await schemaSvc.Save(dto, token);
            entity = dto.Settings.Entity!;
        }

        async Task SaveMainTable()
        {
            if (cols.Length > 0) //if table exists, alter table add columns
            {
                var columnDefinitions = entity.AddedColumnDefinitions(cols);
                if (columnDefinitions.Length > 0)
                {
                    await executor.AlterTableAddColumns(entity.TableName, columnDefinitions,
                        token);
                }
            }
            else
            {
                await executor.CreateTable(entity.TableName, entity.Definitions().EnsureDeleted(),
                    token);
            }
        }

        async Task CreateCrosstables()
        {
            foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Crosstable))
            {
                await CreateCrosstable(entity.ToLoadedEntity(),
                    attribute.ToLoaded(entity.TableName), token);
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

    private async Task<Result<LoadedAttribute>> LoadLookup(LoadedAttribute attr, CancellationToken token)
    {
        if (attr.Lookup is not null)
        {
            return attr;
        }

        var (_, isFailed, value, _) = await GetEntity(attr.GetLookupTarget(), token);
        if (isFailed)
        {
            return Result.Fail(
                $"not find entity by name {attr.GetLookupTarget()} for lookup {attr.AddTableModifier()}");
        }

        return attr with { Lookup = value.ToLoadedEntity() };
    }

    private async Task<Result<LoadedAttribute>> LoadCrosstable(LoadedEntity entity, LoadedAttribute attr,
        CancellationToken token)
    {
        if (attr.Crosstable is not null)
        {
            return attr;
        }

        var (_, _, target, getErr) = await GetEntity(attr.GetCrosstableTarget(), token);
        if (getErr is not null)
        {
            return Result.Fail($"not find entity by name {attr.GetCrosstableTarget()}, err = {getErr}");
        }

        var (_, _, loadedTarget, loadErr) = await LoadAllRelated(target.ToLoadedEntity(), true, token);
        if (loadErr is not null)
        {
            return Result.Fail(loadErr);
        }

        return attr with { Crosstable = CrosstableHelper.Crosstable(entity, loadedTarget!, attr) };
    }

    public async Task<Result<LoadedAttribute>> LoadOneRelated(LoadedEntity entity, LoadedAttribute attr,
        CancellationToken token)
    {
        return attr.Type switch
        {
            DisplayType.Crosstable => await LoadCrosstable(entity, attr, token),
            DisplayType.Lookup => await LoadLookup(attr, token),
            _ => attr
        };
    }

    //omitCrosstable: omit  circular reference
    private async Task<Result<LoadedEntity>> LoadAllRelated(LoadedEntity entity, bool omitCrosstable,
        CancellationToken token)
    {
        var lst = new List<LoadedAttribute>();

        foreach (var attribute in entity.Attributes)
        {
            switch (attribute)
            {
                case { Type: DisplayType.Lookup } or { Type: DisplayType.Crosstable } when !omitCrosstable:
                    var (_, _, value, errors) = await LoadOneRelated(entity, attribute, token);
                    if (errors is not null)
                    {
                        return Result.Fail(errors);
                    }

                    lst.Add(value);
                    break;

                default:
                    lst.Add(attribute);
                    break;
            }
        }

        return entity with { Attributes = [..lst] };

    }

    private async Task CreateCrosstable(LoadedEntity entity, LoadedAttribute attribute,
        CancellationToken cancellationToken)
    {
        var targetEntity = CheckResult(await GetLoadedEntity(attribute.GetCrosstableTarget(), cancellationToken));
        var crossTable = CrosstableHelper.Crosstable(entity, targetEntity, attribute);
        var columns =
            await executor.GetColumnDefinitions(crossTable.CrossEntity.TableName, cancellationToken);
        if (columns.Length == 0)
        {
            await executor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions(),
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
        NotNull(entity.Attributes.FindOneAttr(entity.TitleAttribute))
            .ValOrThrow($"`{entity.TitleAttribute}` was not in attributes list");
        foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Lookup))
        {
            await CheckLookup(attribute, cancellationToken);
        }
    }

    public async Task<Schema> AddOrUpdate(Entity entity, CancellationToken cancellationToken)
    {
        var find = await schemaSvc.GetByNameDefault(entity.Name, SchemaType.Entity, cancellationToken);
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