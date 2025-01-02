using System.Text.Json;
using FluentCMS.Cms.Models;
using FluentResults;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;
namespace FluentCMS.Cms.Services;

public sealed class SchemaService(
    IDao dao,
    KateQueryExecutor queryExecutor,
    HookRegistry hook,
    IServiceProvider provider
) : ISchemaService
{
    public async Task<Schema[]> AllWithAction(string type, CancellationToken ct = default)
    {
        var res = await hook.SchemaPreGetAll.Trigger(provider, new SchemaPreGetAllArgs(null));
        IEnumerable<string>? names = res.OutSchemaNames?.Length > 0 ? res.OutSchemaNames : null;
        return await All(type, names, ct);
    }

    public async Task<Schema[]> All(string type, IEnumerable<string>? names,
        CancellationToken ct = default)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(Type, type);
        }

        if (names is not null)
        {
            query.WhereIn(Name, names);
        }

        var items = await queryExecutor.Many(query, ct);
        return items.Select(x => ParseSchema(x).Ok()).ToArray();
    }

    public async Task<Schema?> ByIdWithAction(int id, CancellationToken ct = default)
    {
        var schema = await ById(id, ct);
        if (schema is not null)
        {
            await hook.SchemaPostGetSingle.Trigger(provider, new SchemaPostGetSingleArgs(schema));
        }

        return schema;

    }

    public async Task<Schema?> ById(int id, CancellationToken ct = default)
    {
        var query = BaseQuery().Where(Id, id);
        var item = await queryExecutor.One(query, ct);
        var res = ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNameDefault(string name, string type, CancellationToken ct = default)
    {
        var query = BaseQuery().Where(Name, name).Where(Type, type);
        var item = await queryExecutor.One(query, ct);
        var res = ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNamePrefixDefault(string name, string type,
        CancellationToken ct = default)
    {
        var query = BaseQuery().WhereStarts(Name, name).Where(Type, type);
        var item = await queryExecutor.One(query, ct);

        var res = ParseSchema(item);

        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema> SaveWithAction(Schema schema, CancellationToken ct)
    {
        (await NameNotTakenByOther(schema, ct)).Ok();
        schema = (await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema))).RefSchema;
        schema = await Save(schema, ct);
        await hook.SchemaPostSave.Trigger(provider, new SchemaPostSaveArgs(schema));
        return schema;
    }

    public async Task<Schema> AddOrUpdateByNameWithAction(Schema schema,  CancellationToken ct)
    {
        var find = await GetByNameDefault(schema.Name, schema.Type, ct);
        if (find is not null)
        {
            schema = schema with { Id = find.Id };
        }

        var res = await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema));
        return await Save(res.RefSchema,ct);
    }

    public async Task Delete(int id, CancellationToken ct)
    {
        var res = await hook.SchemaPreDel.Trigger(provider, new SchemaPreDelArgs(id));
        var query = new SqlKata.Query(TableName).Where(Id, res.SchemaId).AsUpdate([Deleted], [true]);
        await queryExecutor.Exec(query, ct);
    }

    public async Task EnsureTopMenuBar(CancellationToken ct )
    {
        var query = BaseQuery().Where(Name, SchemaName.TopMenuBar);
        var item = await queryExecutor.One(query, ct);
        if (item is not null)
        {
            return;
        }

        var menuBarSchema = new Schema
        (
            Name: SchemaName.TopMenuBar,
            Type: SchemaType.Menu,
            Settings: new Settings
            (
                Menu: new Menu
                (
                    Name: SchemaName.TopMenuBar,
                    MenuItems: []
                )
            )
        );

        await Save(menuBarSchema, ct);
    }

    public async Task EnsureSchemaTable(CancellationToken ct)
    {
        var cols = await dao.GetColumnDefinitions(TableName, ct);
        if (cols.Length > 0)
        {
            return;
        }

        var entity = new Entity
        (
            TableName: TableName,
            Attributes:
            [
                new Attribute(Name),
                new Attribute(Type),
                new Attribute
                (
                    Field: Settings,
                    DataType: DataType.Text
                ),
                new Attribute(CreatedBy)
            ]
        );
        entity = entity.WithDefaultAttr();
        cols = entity.Attributes.Select(x => new Column(x.Field, x.DataType)).ToArray();
        await dao.CreateTable(entity.TableName, cols.EnsureDeleted(),ct);
    }

    public async Task RemoveEntityInTopMenuBar(Entity entity, CancellationToken ct)
    {
        var menuBarSchema = await GetByNameDefault(SchemaName.TopMenuBar, SchemaType.Menu, ct) ??
                            throw new ResultException("not find top menu bar");
        var menuBar = menuBarSchema.Settings.Menu;
        if (menuBar is null)
        {
            return;
        }

        var link = "/entities/" + entity.Name;
        var menus = menuBar.MenuItems.Where(x => x.Url != link);
        menuBar = menuBar with { MenuItems = [..menus] };
        menuBarSchema = menuBarSchema with { Settings = new Settings(Menu: menuBar) };
        await Save(menuBarSchema, ct);
    }


    public async Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken ct)
    {
        var menuBarSchema = await GetByNameDefault(SchemaName.TopMenuBar, SchemaType.Menu, ct) ??
                            throw new ResultException("not find top menu bar");
        var menuBar = menuBarSchema.Settings.Menu;
        if (menuBar is not null)
        {
            var link = "/entities/" + entity.Name;
            var menuItem = menuBar.MenuItems.FirstOrDefault(me => me.Url == link);
            if (menuItem is null)
            {
                menuBar = menuBar with
                {
                    MenuItems =
                    [
                        ..menuBar.MenuItems, new MenuItem(Icon: "pi-bolt", Url: link, Label: entity.Title)
                    ]
                };
            }

            menuBarSchema = menuBarSchema with { Settings = new Settings(Menu: menuBar) };
            await Save(menuBarSchema,ct);
        }
    }

    private const string TableName = "__schemas";
    private const string Id = "id";
    private const string Name = "name";
    private const string Type = "type";
    private const string Settings = "settings";
    private const string Deleted = "deleted";
    private const string CreatedBy = "created_by";

    private static string[] Fields() => [Id, Name, Type, Settings, CreatedBy];

    private static SqlKata.Query BaseQuery()
    {
        return new SqlKata.Query(TableName).Select(Fields()).Where(Deleted, false);
    }

    public async Task<Schema> Save(Schema dto,CancellationToken ct)
    {
        if (dto.Id == 0)
        {
            var record = new Dictionary<string, object>
            {
                { Name, dto.Name },
                { Type, dto.Type },
                { Settings, JsonSerializer.Serialize(dto.Settings) },
                { CreatedBy, dto.CreatedBy }
            };
            var query = new SqlKata.Query(TableName).AsInsert(record, true);
            dto = dto with { Id = await queryExecutor.Exec(query,ct) };
        }
        else
        {
            var query = new SqlKata.Query(TableName)
                .Where(Id, dto.Id)
                .AsUpdate(
                    [Name, Type, Settings],
                    [dto.Name, dto.Type, JsonSerializer.Serialize(dto.Settings)]
                );
            await queryExecutor.Exec(query, ct);
        }

        return dto;
    }

    private static Result<Schema> ParseSchema(Record? record)
    {
        if (record is null)
        {
            return Result.Fail("Can not parse schema, input record is null");
        }

        return Result.Try(() =>
        {
            record = record.ToDictionary(pair => pair.Key.ToLower(), pair => pair.Value);
            var s = JsonSerializer.Deserialize<Settings>((string)record[Settings]);
            return new Schema
            (
                Name: (string)record[Name],
                Type: (string)record[Type],
                Settings: s!,
                CreatedBy: (string)record[CreatedBy],
                Id: record[Id] switch
                {
                    int val => val,
                    long val => (int)val,
                    _ => 0
                }
            );
        });
    }

    public async Task<Result> NameNotTakenByOther(Schema schema, CancellationToken ct)
    {
        var query = BaseQuery().Where(Name, schema.Name).Where(Type, schema.Type)
            .WhereNot(Id, schema.Id);
        var count = await queryExecutor.Count(query, ct);
        return count == 0 ? Result.Ok() : Result.Fail($"the schema name {schema.Name} was taken by other schema");
    }
    
    
}
