using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.HookFactory;
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
    KeyValueCache<ImmutableArray<Entity>> entityCache,
HookRegistry hook,
    IServiceProvider provider
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

    public bool ResolveVal(LoadedAttribute attr, string v, out ValidValue result)
    {
        var colType = attr.DataType == DataType.Lookup ? attr.Lookup!.PrimaryKeyAttribute.DataType : attr.DataType;
        result = dao.TryParseDataType(v, colType, out var val) switch
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
            switch (attr.DataType)
            {
                case DataType.Junction:
                    entity = attr.Junction!.TargetEntity;
                    break;
                case DataType.Lookup:
                    entity = attr.Lookup!;
                    break;
                case DataType.Collection:
                    entity = attr.Collection!.TargetEntity;
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
            Attributes: [..cols.Select(x=>AttributeHelper.ToAttribute(x.Name,x.Type))]
        );
    }

    public async Task SaveTableDefine(Entity entity, CancellationToken token = default)
    {
        await SaveTableDefine(ToSchema(entity), token);
    }

    public async Task<Schema> SaveTableDefine(Schema schema, CancellationToken ct = default)
    {
        (await schemaSvc.NameNotTakenByOther(schema, ct)).Ok();

        schema = schema with
        {
            Settings = new Settings(
                (schema.Settings.Entity ?? throw new ResultException("invalid payload")).WithDefaultAttr()
            )
        };
        await VerifyEntity(schema.Settings.Entity!, ct);
        
        var cols = await dao.GetColumnDefinitions(schema.Settings.Entity!.TableName, ct);
        EnsureTableNotExist(schema,cols).Ok();
        
        schema = (await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema))).RefSchema;
        
        using var tx = await dao.BeginTransaction();
        try
        {
            schema = await schemaSvc.Save(schema, ct);
            await CreateJunctions(schema,ct);
            await CreateMainTable(schema,cols,ct);
            await schemaSvc.EnsureEntityInTopMenuBar(schema.Settings.Entity!, ct);
            tx.Commit();
            return schema;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
        finally
        {
            dao.EndTransaction();
            await entityCache.Remove("", ct);
            await hook.SchemaPostSave.Trigger(provider, new SchemaPostSaveArgs(schema));
        }
    }

    private async Task CreateMainTable(Schema schema, Column[] columns,CancellationToken ct)
    {
        var entity = schema.Settings.Entity!;
        if (columns.Length > 0) //if table exists, alter table add columns
        {
            var set = columns.Select(x => x.Name.ToLower()).ToHashSet();
            var missing = entity.Attributes.GetLocalAttrs().Where(c => !set.Contains(c.Field)).ToArray();
            if (missing.Length > 0)
            {
                var missingCols = await ToColumns(missing);
                await dao.AddColumns(entity.TableName, missingCols, ct);
            }
        }
        else
        {
            var newColumns = await ToColumns(entity.Attributes.GetLocalAttrs());
            await dao.CreateTable(entity.TableName, newColumns.EnsureDeleted(), ct);
        }
    }

    private async Task CreateJunctions(Schema schema, CancellationToken ct)
    {
        var entity = schema.Settings.Entity!;
        foreach (var attribute in entity.Attributes.GetAttrByType(DataType.Junction))
        {
            await CreateJunction(entity.ToLoadedEntity(), attribute.ToLoaded(entity.TableName), ct);
        }
    }

    private static Result EnsureTableNotExist(Schema schema, Column[] columns)
    {
        var creatingNewEntity = schema.Id == 0;
        var tableExists = columns.Length > 0;
        
        return creatingNewEntity && tableExists
            ? Result.Fail($"Fail to add new entity, the table {schema.Settings.Entity!.TableName} already exists")
            : Result.Ok();
    }

    private async Task<Column[]> ToColumns(IEnumerable<Attribute> attributes)
    {
        var ret = new List<Column>();
        foreach (var attribute in attributes)
        {
            ret.Add(await ToColumn(attribute));
        }
        return ret.ToArray();
    }

    private async Task<Column> ToColumn(Attribute attribute)
    {
        var dataType= attribute.DataType switch
        {
            DataType.Junction => throw new Exception("Junction attribute does not map to database"),
            DataType.Lookup => (await GetLookupEntity(attribute).Map(x=> x.Attributes.FindOneAttr(x.PrimaryKey)!.DataType)).Ok(),
            _ => attribute.DataType
        };
        return new Column(attribute.Field, dataType);
    }

    private async Task<Result<Entity>> GetLookupEntity(Attribute attribute, CancellationToken ct = default)
        => attribute.GetLookupTarget(out var entity)
            ? await GetEntity(entity,ct)
            : Result.Fail($"Lookup target was not set to {attribute.Field}");

    private async Task<Result<LoadedAttribute>> LoadLookup(
        LoadedAttribute attr, CancellationToken ct
    ) => attr.Lookup switch
    {
        not null => attr,
        _ => await GetLookupEntity(attr, ct).Map(x => attr with { Lookup = x.ToLoadedEntity() })
    };
    
    private async Task<Result<LoadedAttribute>> LoadCollection(
        LoadedAttribute attr, CancellationToken ct
    )
    {
        return attr.Collection switch
        {
            not null => attr,
            _ => await GetCollection(attr,ct).Map(c=>attr with{Collection =c })
        };
    }

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
        return attr.DataType switch
        {
            DataType.Junction => await LoadJunction(entity, attr,visited, ct),
            DataType.Lookup => await LoadLookup(attr, ct),
            DataType.Collection => await LoadCollection(attr,ct),
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
            if (!attribute.IsCompound())
            {
                lst.Add(attribute);
            }
            else
            {
                var (isSuccess, _, loadedAttribute, errors) = await LoadCompoundAttribute(entity, attribute, visited, ct);
                if (!isSuccess)
                {
                    return Result.Fail(errors);
                }
                lst.Add(loadedAttribute);
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
            await dao.GetColumnDefinitions(junction.JunctionEntity.TableName, ct);
        if (columns.Length == 0)
        {
            var cols =await ToColumns(junction.JunctionEntity.Attributes);
            await dao.CreateTable(junction.JunctionEntity.TableName, cols.EnsureDeleted(), ct);
        }
    }


    private async Task<Result<Collection>> GetCollection(Attribute attr, CancellationToken ct)
    {
        if (!attr.GetCollectionTarget(out var entityName, out var linkAttrName))
        {
            return Result.Fail($"Collection target entity or collection target entity link attribute was not set for attribute `{attr.Field}`");
        }

        var  entity= await GetEntity(entityName, ct).Ok();
        var loadAttribute = entity.Attributes.FindOneAttr(linkAttrName);
        if (loadAttribute is null) return Result.Fail($"Not found [{linkAttrName}] from entity [{entityName}]");
        return new Collection(entity.ToLoadedEntity(),loadAttribute.ToLoaded(entity.TableName));
    
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
        
        foreach (var attribute in entity.Attributes.GetAttrByType(DataType.Lookup))
        {
            _= await GetLookupEntity(attribute, ct);
        }
        
        foreach (var attribute in entity.Attributes.GetAttrByType(DataType.Collection))
        {
            _= await GetCollection(attribute, ct);
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