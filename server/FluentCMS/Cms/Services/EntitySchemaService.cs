using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public sealed class EntitySchemaService(
    ISchemaService schemaSvc,
    IDefinitionExecutor executor,
    KeyValueCache<ImmutableArray<Entity>> entityCache
) : IEntitySchemaService
{
    public ValueTask<ImmutableArray<Entity>> AllEntities(CancellationToken ct = default)
    {
        return entityCache.GetOrSet("", async token =>
        {
            var schemas = await schemaSvc.All(SchemaType.Entity, null, token);
            var entities = schemas
                .Where(x => x.Settings.Entity is not null)
                .Select(x => x.Settings.Entity!);
            return [..entities];
        }, ct);
    }

    public bool ResolveVal(Attribute attr, string v, out ValidValue result)
    {
        result = executor.TryParseDataType(v, attr.DataType, out var val) switch
        {
            true => new ValidValue(S:val!.S, I:val.I,D: val.D),
            _ => ValidValue.EmptyValue
        };
        return result != ValidValue.EmptyValue;
    }

    public async Task<Result<AttributeVector>> ResolveVector(LoadedEntity entity, string fieldName)
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

            var res = await LoadCompoundAttribute(entity, attr,[], default);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            attr = res.Value;
            switch (attr.Type)
            {
                case DisplayType.Junction:
                    entity = attr.Junction!.TargetEntity;
                    break;
                case DisplayType.Lookup:
                    entity = attr.Lookup!;
                    break;
                default:
                    return Result.Fail($"Can not resolve [{fieldName}], [{attr.Field}] is not a composite type");
            }

            attributes.Add(attr);
        }

        return new AttributeVector(fieldName, prefix, [..attributes], attr!);

    }

    public async Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken token = default)
    {
        var (_, isFailed, entity, errors) = await GetEntity(name, token);
        if (isFailed)
        {
            return Result.Fail(errors);
        }

        var ret = await LoadCompoundAttributes(entity.ToLoadedEntity(), [], token);
        return ret;
    }

    private async Task<Result<Entity>> GetEntity(string name, CancellationToken token = default)
    {
        var item = await schemaSvc.GetByNameDefault(name, SchemaType.Entity, token);
        if (item is null)
        {
            return Result.Fail($"can not find entity {name} ");
        }

        var entity = item.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"entity {name} is invalid");
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


    public async Task<Schema> SaveTableDefine(Schema dto, CancellationToken ct = default)
    {
        (await schemaSvc.NameNotTakenByOther(dto, ct)).Ok();
        var entity = (dto.Settings.Entity?? throw new ResultException("invalid payload"));
        entity = entity.WithDefaultAttr();
        var cols = await executor.GetColumnDefinitions(entity.TableName, ct);
        EnsureTableNotExist().Ok();
        await VerifyEntity(entity, ct);
        await SaveSchema(); //need  to save first because it will call trigger
        await CreateJunctions();
        await SaveMainTable();
        await schemaSvc.EnsureEntityInTopMenuBar(entity, ct);
        await entityCache.Remove("",ct);
        return dto;

        async Task SaveSchema()
        {
            dto = dto with { Settings = new Settings(entity) };
            dto = await schemaSvc.SaveWithAction(dto, ct);
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
                        ct);
                }
            }
            else
            {
                await executor.CreateTable(entity.TableName, entity.Definitions().EnsureDeleted(),
                    ct);
            }
        }

        async Task CreateJunctions()
        {
            foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Junction))
            {
                await this.CreateJunction(entity.ToLoadedEntity(),
                    attribute.ToLoaded(entity.TableName), ct);
            }
        }

        Result EnsureTableNotExist()
        {
            var creatingNewEntity = dto.Id == 0;
            var tableExists = cols.Length > 0;
            return creatingNewEntity && tableExists
                ? Result.Fail($"Fail to add new entity, the table {entity.TableName} already exists")
                : Result.Ok();
        }
    }

    private async Task<Result<LoadedAttribute>> LoadLookup(LoadedAttribute attr, CancellationToken ct)
    {
        if (attr.Lookup is not null)
        {
            return attr;
        }

        if (!attr.GetLookupTarget(out var lookupName))
        {
            return Result.Fail($"Lookup Option was not set for attribute `{attr.Field}`");
        }

        var (_, isFailed, value, _) = await GetEntity(lookupName, ct);
        if (isFailed)
        {
            return Result.Fail(
                $"not find entity by name {lookupName} for lookup {attr.Field}");
        }

        return attr with { Lookup = value.ToLoadedEntity() };
    }

    private async Task<Result<LoadedAttribute>> LoadJunction(
        LoadedEntity entity, 
        LoadedAttribute attr,
        HashSet<string> visited,
        CancellationToken token)
    {
        if (attr.Junction is not null)
        {
            return attr;
        }

        if (!attr.GetJunctionTarget(out var targetName))
        {
            return Result.Fail($"Junction Option was not set for attribute `{entity.Name}.{attr.Field}`");
        }
        
        var tableName = JunctionHelper.GetJunctionTableName(entity.Name, targetName);
        if (!visited.Add(tableName))
        {
            return attr;
        }

        var (_, _, target, getErr) = await GetEntity(targetName, token);
        if (getErr is not null)
        {
            return Result.Fail($"not find entity by name {targetName}, err = {getErr}");
        }

        var (_, _, loadedTarget, loadErr) = await LoadCompoundAttributes(target.ToLoadedEntity(), visited, token);
        if (loadErr is not null)
        {
            return Result.Fail(loadErr);
        }

        return attr with { Junction = JunctionHelper.Junction(entity, loadedTarget!, attr) };
    }

    public async Task<Result<LoadedAttribute>> LoadCompoundAttribute(
        LoadedEntity entity, 
        LoadedAttribute attr,
        HashSet<string> visited,
        CancellationToken ct)
    {
        return attr.Type switch
        {
            DisplayType.Junction => await LoadJunction(entity, attr,visited, ct),
            DisplayType.Lookup => await LoadLookup(attr, ct),
            _ => attr
        };
    }

    public async Task Delete(Schema schema, CancellationToken ct)
    {
        await schemaSvc.Delete(schema.Id, ct);
        if (schema.Settings.Entity is not null) await schemaSvc.RemoveEntityInTopMenuBar(schema.Settings.Entity, ct);
        await entityCache.Remove("",ct);
    }

    public async Task<Schema> Save(Schema schema, CancellationToken ct)
    {
        var ret = await schemaSvc.SaveWithAction(schema, ct);
        await  entityCache.Remove("",ct);
        return ret;
    }

    private async Task<Result<LoadedEntity>> LoadCompoundAttributes(
        LoadedEntity entity, HashSet<string> visited, CancellationToken ct)
    {
        var lst = new List<LoadedAttribute>();

        foreach (var attribute in entity.Attributes)
        {
            switch (attribute)
            {
                case { Type: DisplayType.Lookup  or DisplayType.Junction }:
                    var (_, _, value, errors) = await LoadCompoundAttribute(entity, attribute, visited,ct);
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

    private async Task CreateJunction(LoadedEntity entity, LoadedAttribute attr, CancellationToken ct)
    {
        if (!attr.GetJunctionTarget(out var name))
        {
            throw new Exception($"Junction Option was not set for attribute `{entity.Name}.{attr.Field}`");
        }

        var targetEntity = (await GetLoadedEntity(name, ct)).Ok();
        var junction = JunctionHelper.Junction(entity, targetEntity, attr);
        var columns =
            await executor.GetColumnDefinitions(junction.JunctionEntity.TableName, ct);
        if (columns.Length == 0)
        {
            await executor.CreateTable(junction.JunctionEntity.TableName, junction.GetColumnDefinitions(),
                ct);
        }
    }

    private async Task CheckLookup(Attribute attr, CancellationToken ct)
    {
        if (attr.DataType != DataType.Int) throw new ResultException("lookup datatype should be int");
        if (!attr.GetLookupTarget(out var lookupName))
            throw new ResultException($"Lookup Option was not set for attribute `{attr.Field}`");

        _ = await GetEntity(lookupName, ct) ??
            throw new ResultException($"not find entity by name {lookupName}");
    }

    private async Task VerifyEntity(Entity entity, CancellationToken ct)
    {
        _ = entity.Attributes.FindOneAttr(entity.TitleAttribute) ??
            throw new ResultException($"`{entity.TitleAttribute}` was not in attributes list");
        foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Lookup))
        {
            await CheckLookup(attribute, ct);
        }
    }

    public async Task<Schema> AddOrUpdateByName(Entity entity, CancellationToken ct)
    {
        var find = await schemaSvc.GetByNameDefault(entity.Name, SchemaType.Entity, ct);
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
        return await SaveTableDefine(schema, ct);
    }
}