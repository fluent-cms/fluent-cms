using System.Collections.Immutable;
using FluentCMS.Core.Cache;
using FluentCMS.Core.HookFactory;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using FluentResults.Extensions;
using Attribute = FluentCMS.Core.Descriptors.Attribute;

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

    public async Task<Schema> AddOrUpdateByName(Entity entity, CancellationToken ct)
    {
        var find = await schemaSvc.GetByNameDefault(entity.Name, SchemaType.Entity, ct);
        var schema = ToSchema(entity, find?.Id ?? 0);
        return await SaveTableDefine(schema, ct);
    }


    public async Task<Schema> Save(Schema schema, CancellationToken ct)
    {
        var ret = await schemaSvc.SaveWithAction(schema, ct);
        await entityCache.Remove("", ct);
        return ret;
    }

    public async Task Delete(Schema schema, CancellationToken ct)
    {
        await schemaSvc.Delete(schema.Id, ct);
        if (schema.Settings.Entity is not null) await schemaSvc.RemoveEntityInTopMenuBar(schema.Settings.Entity, ct);
        await entityCache.Remove("", ct);
    }


    public async Task<Result<LoadedEntity>> LoadEntity(string name, CancellationToken ct = default)
        => await GetEntity(name, ct).Bind(async x => await LoadAttributes(x.ToLoadedEntity(), ct));

    private async Task<Result<LoadedAttribute>> LoadSingleAttribute(
        LoadedEntity entity,
        LoadedAttribute attr,
        CancellationToken ct = default
    ) => attr.DataType switch
    {
        DataType.Junction => await LoadJunction(entity, attr, ct),
        DataType.Lookup => await LoadLookup(attr, ct),
        DataType.Collection => await LoadCollection(entity, attr,ct),
        _ => attr
    };

    public async Task<Entity?> GetTableDefine(string table, CancellationToken token)
    {
        var cols = await dao.GetColumnDefinitions(table, token);
        return new Entity
        (
            Attributes: [..cols.Select(x => AttributeHelper.ToAttribute(x.Name, x.Type))]
        );
    }

    public async Task SaveTableDefine(Entity entity, CancellationToken token = default)
    {
        await SaveTableDefine(ToSchema(entity), token);
    }

    public async Task<Schema> SaveTableDefine(Schema schema, CancellationToken ct = default)
    {
        
        //hook function might change the schema
        schema = (await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema))).RefSchema;
        schema = WithDefaultAttr(schema);
        VerifyEntity(schema.Settings.Entity!);
        
        (await schemaSvc.NameNotTakenByOther(schema, ct)).Ok();
        var cols = await dao.GetColumnDefinitions(schema.Settings.Entity!.TableName, ct);
        EnsureTableNotExist(schema, cols).Ok();

        using var tx = await dao.BeginTransaction();
        
        try
        {
            schema = await schemaSvc.Save(schema, ct);
            await CreateMainTable(schema.Settings.Entity!, cols, ct);
            await schemaSvc.EnsureEntityInTopMenuBar(schema.Settings.Entity!, ct);
            
            var loadedEntity = await LoadAttributes(schema.Settings.Entity!.ToLoadedEntity(), ct).Ok();
            await CreateLookupForeignKey(loadedEntity, ct);
            await CreateCollectionForeignKey(loadedEntity, ct);
            await CreateJunctions(loadedEntity, ct);
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

        Schema WithDefaultAttr(Schema s)
        {
            var e = s.Settings.Entity ?? throw new ResultException("invalid entity payload");
            return s with
            {
                Settings = new Settings(
                    Entity: e.WithDefaultAttr()
                )
            };
        }
    }

    public bool ResolveVal(LoadedAttribute attr, string v, out ValidValue? result)
    {
        var colType = attr.DataType == DataType.Lookup ? attr.Lookup!.TargetEntity.PrimaryKeyAttribute.DataType : attr.DataType;
        result = dao.TryParseDataType(v, colType, out var val) switch
        {
            true => new ValidValue(S: val!.S, I: val.I, D: val.D),
            _ => null
        };
        return result != null;
    }

    public async Task<Result<AttributeVector>> ResolveVector(LoadedEntity entity, string fieldName)
    {
        var fields = fieldName.Split(".");
        var prefix = string.Join(AttributeVectorConstants.Separator, fields[..^1]);
        var attributes = new List<LoadedAttribute>();
        LoadedAttribute? attr = null;
        for (var i = 0; i < fields.Length; i++)
        {
            //check if fields[i] exists in entity
            if (!(await LoadSingleAttrByName(entity, fields[i])).Try(out attr, out var e))
            {
                return Result.Fail(e);
            }

            //don't put the last attribute to ancestor
            if (i >= fields.Length - 1) continue;
            
            if (!attr.GetEntityLinkDesc().Try(out var link, out  e))
            {
                return Result.Fail(e);
            }

            entity = link.TargetEntity;
            attributes.Add(attr);
        }

        return new AttributeVector(fieldName, prefix, [..attributes], attr!);
    }

    public async Task<Result<LoadedAttribute>> LoadSingleAttrByName(LoadedEntity entity, string attrName,
        CancellationToken ct = default)
    {
        var loadedAttr = entity.Attributes.FirstOrDefault(x=>x.Field==attrName);
        if (loadedAttr is null)
            return Result.Fail($"Load single attribute fail, cannot find [{attrName}] in [{entity.Name}]");

        return await LoadSingleAttribute(entity, loadedAttr, ct);
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

    private async Task CreateMainTable(Entity entity, Column[] columns, CancellationToken ct)
    {
        if (columns.Length > 0) //if table exists, alter table add columns
        {
            var set = columns.Select(x => x.Name.ToLower()).ToHashSet();
            var missing = entity.Attributes.Where(c => c.IsLocal()&& !set.Contains(c.Field)).ToArray();
            if (missing.Length > 0)
            {
                var missingCols = await ToColumns(missing);
                await dao.AddColumns(entity.TableName, missingCols, ct);
            }
        }
        else
        {
            var newColumns = await ToColumns(entity.Attributes.Where(x=>x.IsLocal()));
            await dao.CreateTable(entity.TableName, newColumns.EnsureDeleted(), ct);
        }
    }

    private async Task CreateLookupForeignKey(LoadedEntity entity,CancellationToken ct)
    {
        foreach (var attr in entity.Attributes.Where(attr=>attr.DataType == DataType.Lookup))
        {
            var targetEntity = attr.Lookup!.TargetEntity;
            await dao.CreateForeignKey(entity.TableName, attr.Field, targetEntity.TableName, targetEntity.PrimaryKey, ct);
        }
    }
    private async Task CreateCollectionForeignKey(LoadedEntity entity,CancellationToken ct)
    {
        foreach (var attr in entity.Attributes.Where(attr=>attr.DataType == DataType.Collection))
        {
            var collection = attr.Collection!;
            await dao.CreateForeignKey(collection.TargetEntity.TableName, collection.LinkAttribute.Field, entity.TableName, entity.PrimaryKey, ct);
        }
    }

    private async Task CreateJunctions(LoadedEntity entity, CancellationToken ct)
    {
        foreach (var attribute in entity.Attributes.Where(x => x.DataType == DataType.Junction))
        {
            var junction = attribute.Junction!;
            var columns = await dao.GetColumnDefinitions(junction.JunctionEntity.TableName, ct);
            if (columns.Length == 0)
            {
                var cols = await ToColumns(junction.JunctionEntity.Attributes);
                await dao.CreateTable(junction.JunctionEntity.TableName, cols.EnsureDeleted(), ct);
                await dao.CreateForeignKey(
                    table: junction.JunctionEntity.TableName,
                    col: junction.SourceAttribute.Field,
                    refTable: junction.SourceEntity.TableName,
                    refCol: junction.SourceEntity.PrimaryKey,
                    ct);

                await dao.CreateForeignKey(
                    table: junction.JunctionEntity.TableName,
                    col: junction.TargetAttribute.Field,
                    refTable: junction.TargetEntity.TableName,
                    refCol: junction.TargetEntity.PrimaryKey,
                    ct);
            }
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

    private async Task<Result<Entity>> GetLookupEntity(Attribute attribute, CancellationToken ct = default)
        => attribute.GetLookupTarget(out var entity)
            ? await GetEntity(entity, ct)
            : Result.Fail($"Lookup target was not set to Attribute [{attribute.Field}]");

    private async Task<Result<LoadedAttribute>> LoadLookup(
        LoadedAttribute attr, CancellationToken ct
    ) => attr.Lookup switch
    {
        not null => attr,
        _ => await GetLookupEntity(attr, ct).Map(x => attr with { Lookup = new Lookup(x.ToLoadedEntity())})
    };

    private async Task<Result<LoadedAttribute>> LoadCollection( 
        LoadedEntity sourceEntity, LoadedAttribute attr, CancellationToken ct
    )
    {
        if (attr.Collection is not null) return attr;
        
        if (!attr.GetCollectionTarget(out var entityName, out var linkAttrName))
            return Result.Fail( $"Target of Collection attribute [{attr.Field}] not set.");

        return await GetEntity(entityName, ct).Bind(async entity =>
        {

            var loadedEntity = entity.ToLoadedEntity();
            if (!(await LoadLookups(loadedEntity, ct)).Try(out loadedEntity, out var err))
            {
                return Result.Fail(err);
            }

            var loadAttribute = loadedEntity.Attributes.FirstOrDefault(x=>x.Field ==linkAttrName);
            if (loadAttribute is null) return Result.Fail($"Not found [{linkAttrName}] from entity [{entityName}]");
            
            var collection = new Collection(sourceEntity, loadedEntity,loadAttribute );
            return Result.Ok(collection);
        }).Map(c => attr with{Collection = c});
    }

    private Task<Result<LoadedEntity>> LoadLookups(LoadedEntity entity, CancellationToken ct = default)
        => entity.Attributes
            .ShortcutMap(async attr =>
                attr is { DataType: DataType.Lookup} ? await LoadLookup(attr, ct) : attr)
            .Map(x => entity with { Attributes = [..x] });

    private async Task<Result<LoadedAttribute>> LoadJunction(
        LoadedEntity entity, LoadedAttribute attr, CancellationToken ct
    )
    {
        if (attr.Junction is not null) return attr;

        if (!attr.GetJunctionTarget(out var targetName))
            return Result.Fail($"Junction Option was not set for attribute `{entity.Name}.{attr.Field}`");

        return await GetEntity(targetName, ct)
            .Map(e => e.ToLoadedEntity())
            .Bind(e => LoadLookups(e, ct))
            .Map(x => attr with { Junction = JunctionHelper.Junction(entity, x, attr) })
            .OnFail($"Failed to load Junction for attribute {attr.Field}");
    }

    private Task<Result<LoadedEntity>> LoadAttributes(
        LoadedEntity entity, CancellationToken ct
    ) => entity.Attributes
        .ShortcutMap(x => LoadSingleAttribute(entity, x,ct))
        .Map(x => entity with { Attributes = [..x] });

    private void VerifyEntity(Entity entity)
    {
        foreach (var attr in entity.Attributes)
        {
            if (!DataTypeHelper.ValidTypeMap.Contains((attr.DataType, attr.DisplayType)))
            {
                throw new ResultException(
                    $"[{attr.Field}] Data type [{attr.DataType}] with Display Type [{attr.DisplayType}] is not supported .");
            }
        }

        if (string.IsNullOrEmpty(entity.TableName)) throw new ResultException("table name should not be empty");
        if (string.IsNullOrEmpty(entity.TitleAttribute)) throw new ResultException("title should not be empty");
        if (string.IsNullOrEmpty(entity.PrimaryKey)) throw new ResultException("primary key should not be empty");

        if (entity.DefaultPageSize < 1) throw new ResultException("default page size should be greater than 0");

        _ = entity.Attributes.FirstOrDefault(x=>x.Field ==entity.PrimaryKey) ??
            throw new ResultException($"`{entity.PrimaryKey}` was not in attributes list");

        _ = entity.Attributes.FirstOrDefault(x=>x.Field==entity.TitleAttribute) ??
            throw new ResultException($"`{entity.TitleAttribute}` was not in attributes list");

    }


    private Schema ToSchema(
        Entity entity, int id = 0
    ) => new(
        Id: id,
        Name: entity.Name,
        Type: SchemaType.Entity,
        Settings: new Settings
        (
            Entity: entity
        ),
        CreatedBy: ""
    );

    private async Task<Column[]> ToColumns(IEnumerable<Attribute> attributes)
    {
        var ret = new List<Column>();
        foreach (var attribute in attributes)
        {
            ret.Add(await ToColumn(attribute));
        }

        return ret.ToArray();

        async Task<Column> ToColumn(Attribute attribute)
        {
            var dataType = attribute.DataType switch
            {
                DataType.Junction or DataType.Collection=> throw new Exception("Junction/Collection don't need to map to database"),
                DataType.Lookup => (await GetLookupEntity(attribute)
                    .Map(x => x.Attributes.FirstOrDefault(a=>a.Field == x.PrimaryKey)!.DataType)).Ok(),
                _ => attribute.DataType
            };
            return new Column(attribute.Field, dataType);
        }
    }
}