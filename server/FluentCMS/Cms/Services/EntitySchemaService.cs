using System.Collections.Immutable;
using System.Data;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using FluentResults.Extensions;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public sealed class EntitySchemaService(
    ISchemaService schemaSvc,
    IDao dao,
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
        result = dao.TryParseDataType(v, attr.DataType, out var val) switch
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
                return Result.Fail($"Fail to resolve attribute vector: Cannot find [{field}] in {entity.Name} ");
            }

            if (i == fields.Length - 1) break;

            var res = await LoadCompoundAttribute(entity, attr,[],CancellationToken.None);
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
                    return Result.Fail($"Fail to resolve [{fieldName}], [{attr.Field}] is not a composite type");
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
            return Result.Fail($"Cannot find entity [{name}]");
        }

        var entity = item.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"Entity [{name}] is invalid");
        }

        return entity;
    }

    public async Task<Entity?> GetTableDefine(string table, CancellationToken token)
    {
        var cols = await dao.GetColumnDefinitions(table, token);
        return new Entity
        (
            Attributes: [..cols.Select(AttributeHelper.ToAttribute)]
        );
    }

    public async Task SaveTableDefine(Entity entity, CancellationToken token = default)
    {
        await SaveTableDefine(ToSchema(entity), token);
    }

    public async Task<Schema> SaveTableDefine(Schema dto, CancellationToken ct = default)
    {
        
        (await schemaSvc.NameNotTakenByOther(dto, ct)).Ok();
        var entity = (dto.Settings.Entity?? throw new ResultException("invalid payload"));
        entity = entity.WithDefaultAttr();
        var cols = await dao.GetColumnDefinitions(entity.TableName, ct);
        EnsureTableNotExist().Ok();
        await VerifyEntity(entity, ct);
        
        using var tx = await dao.BeginTransaction();

        try
        {
            await SaveSchema(tx); //need  to save first because it will call trigger
            await CreateJunctions(tx);
            await SaveMainTable(tx);
            await schemaSvc.EnsureEntityInTopMenuBar(entity, ct, tx);

            await entityCache.Remove("", ct);
            tx.Commit();
            return dto;
        }
        catch 
        {
            tx.Rollback();
            throw;
        }


        async Task SaveSchema(IDbTransaction t)
        {
            dto = dto with { Settings = new Settings(entity) };
            dto = await schemaSvc.SaveWithAction(dto, ct,t);
            entity = dto.Settings.Entity!;
        }

        async Task SaveMainTable(IDbTransaction t)
        {
            if (cols.Length > 0) //if table exists, alter table add columns
            {
                var columnDefinitions = entity.AddedColumnDefinitions(cols);
                if (columnDefinitions.Length > 0)
                {
                    await dao.AddColumns(entity.TableName, columnDefinitions, ct,t);
                }
            }
            else
            {
                await dao.CreateTable(entity.TableName, entity.Definitions().EnsureDeleted(), ct,t);
            }
        }

        async Task CreateJunctions(IDbTransaction t)
        {
            foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Junction))
            {
                await CreateJunction(entity.ToLoadedEntity(), attribute.ToLoaded(entity.TableName), ct,t);
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

    private async Task<Result<LoadedAttribute>> LoadLookup(
        LoadedAttribute attr, CancellationToken ct
    ) => attr.Lookup switch
    {
        not null => attr,
        _ => attr.GetLookupTarget(out var lookupTarget)
            ? await GetEntity(lookupTarget, ct)
                .Map(lookup => attr with { Lookup = lookup.ToLoadedEntity() })
                .OnFail("Failed to load lookup")
            : Result.Fail($"Lookup target was not set for attribute `{attr.Field}`")
    };

    private async Task<Result<LoadedAttribute>> LoadJunction(
        LoadedEntity entity, 
        LoadedAttribute attr,
        HashSet<string> visited,
        CancellationToken ct)
    {
        if (attr.Junction is not null) return attr;

        if (!attr.GetJunctionTarget(out var targetName))
        {
            return Result.Fail($"Junction Option was not set for attribute `{entity.Name}.{attr.Field}`");
        }
        
        var tableName = JunctionHelper.GetJunctionTableName(entity.Name, targetName);
        if (!visited.Add(tableName))
        {
            return attr;
        }

        return await GetEntity(targetName, ct)
            .Map(e => e.ToLoadedEntity())
            .Map(x => attr with { Junction = JunctionHelper.Junction(entity, x, attr) })
            .OnFail($"Failed to load Junction for attribute {attr.Field}");
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

    private async Task CreateJunction(LoadedEntity entity, LoadedAttribute attr, CancellationToken ct, IDbTransaction tx)
    {
        if (!attr.GetJunctionTarget(out var name))
        {
            throw new Exception($"Junction Option was not set for attribute `{entity.Name}.{attr.Field}`");
        }

        var targetEntity = (await GetLoadedEntity(name, ct)).Ok();
        var junction = JunctionHelper.Junction(entity, targetEntity, attr);
        var columns =
            await dao.GetColumnDefinitions(junction.JunctionEntity.TableName, ct);
        if (columns.Length == 0)
        {
            await dao.CreateTable(junction.JunctionEntity.TableName, junction.GetColumnDefinitions(), ct,tx);
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
        if (string.IsNullOrEmpty(entity.TableName)) throw new ResultException("table name should not be empty");
        if (string.IsNullOrEmpty(entity.TitleAttribute)) throw new ResultException("title should not be empty");
        if (string.IsNullOrEmpty(entity.PrimaryKey)) throw new ResultException("primary key should not be empty");
        
        if (entity.DefaultPageSize <1) throw new ResultException("default page size should be greater than 0");
        
        _ = entity.Attributes.FindOneAttr(entity.PrimaryKey) ??
            throw new ResultException($"`{entity.PrimaryKey}` was not in attributes list");
        
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
        var schema = ToSchema(entity, find?.Id ?? 0);
        return await SaveTableDefine(schema, ct);
    }

    private Schema ToSchema(Entity entity, int id = 0)
        => new (
            Id: id,
            Name: entity.Name,
            Type: SchemaType.Entity,
            Settings: new Settings
            (
                Entity: entity
            ),
            CreatedBy: ""
        );
}