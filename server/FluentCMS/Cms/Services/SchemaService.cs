using FluentResults;
using FluentCMS.Core.HookFactory;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.ResultExt;

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
        var query = SchemaHelper.BaseQuery();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(SchemaFields.Type, type);
        }

        if (names is not null)
        {
            query.WhereIn(SchemaFields.Name, names);
        }

        var items = await queryExecutor.Many(query, ct);
        return items.Select(x => SchemaHelper.ParseSchema(x).Ok()).ToArray();
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
        var query = SchemaHelper.BaseQuery().Where(SchemaFields.Id, id);
        var item = await queryExecutor.One(query, ct);
        var res = SchemaHelper.ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNameDefault(string name, string type, CancellationToken ct = default)
    {
        var query = SchemaHelper.BaseQuery().Where(SchemaFields.Name, name).Where(SchemaFields.Type, type);
        var item = await queryExecutor.One(query, ct);
        var res = SchemaHelper.ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNamePrefixDefault(string name, string type,
        CancellationToken ct = default)
    {
        var query = SchemaHelper.BaseQuery()
            .WhereStarts(SchemaFields.Name, name)
            .Where(SchemaFields.Type, type);
        var item = await queryExecutor.One(query, ct);

        var res = SchemaHelper.ParseSchema(item);

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

    public async Task<Schema> AddOrUpdateByNameWithAction(Schema schema, CancellationToken ct)
    {
        var find = await GetByNameDefault(schema.Name, schema.Type, ct);
        if (find is not null)
        {
            schema = schema with { Id = find.Id };
        }

        var res = await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema));
        return await Save(res.RefSchema, ct);
    }

    public async Task Delete(int id, CancellationToken ct)
    {
        var res = await hook.SchemaPreDel.Trigger(provider, new SchemaPreDelArgs(id));
        await queryExecutor.Exec(SchemaHelper.SoftDelete(res.SchemaId), ct);
    }

    public async Task EnsureTopMenuBar(CancellationToken ct)
    {
        var query = SchemaHelper.BaseQuery().Where(SchemaFields.Name, SchemaName.TopMenuBar);
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
        var cols = await dao.GetColumnDefinitions(SchemaHelper.TableName, ct);
        if (cols.Length > 0)
        {
            return;
        }

        cols =
        [
            new Column(SchemaFields.Id, ColumnType.Int),
            new Column(SchemaFields.Name, ColumnType.String),
            new Column(SchemaFields.Type, ColumnType.String),
            new Column(SchemaFields.Settings, ColumnType.Text),
            new Column(SchemaFields.Deleted, ColumnType.Int),
            new Column(SchemaFields.CreatedBy, ColumnType.String),
            new Column(FluentCMS.Core.Descriptors.DefaultFields.CreatedAt, ColumnType.Datetime),
            new Column(FluentCMS.Core.Descriptors.DefaultFields.UpdatedAt, ColumnType.Datetime),
        ];
        await dao.CreateTable(SchemaHelper.TableName, cols, ct);
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
            await Save(menuBarSchema, ct);
        }
    }



    public async Task<Schema> Save(Schema dto, CancellationToken ct)
    {
        dto = dto with { Id = await queryExecutor.Exec(dto.Save(), ct) };
        return dto;
    }

    public async Task<Result> NameNotTakenByOther(Schema schema, CancellationToken ct)
    {
        var query = SchemaHelper.BaseQuery()
            .Where(SchemaFields.Name, schema.Name)
            .Where(SchemaFields.Type, schema.Type)
            .WhereNot(SchemaFields.Id, schema.Id);
        var count = await queryExecutor.Count(query, ct);
        return count == 0 ? Result.Ok() : Result.Fail($"the schema name {schema.Name} was taken by other schema");
    }
}
