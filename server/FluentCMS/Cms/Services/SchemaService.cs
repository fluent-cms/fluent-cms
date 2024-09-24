using System.Text.Json;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentResults;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;
namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed  class SchemaService(
    IDefinitionExecutor definitionExecutor,
    KateQueryExecutor kateQueryExecutor,
    HookRegistry hookRegistry,
    IServiceProvider provider
) : ISchemaService
{
    public async Task<Result> NameNotTakenByOther(Schema schema, CancellationToken cancellationToken)
    {
        var query = BaseQuery().Where(ColumnName, schema.Name).Where(ColumnType, schema.Type)
            .WhereNot(ColumnId, schema.Id);
        var count = await kateQueryExecutor.Count(query, cancellationToken);
        return count == 0 ? Result.Ok() : Result.Fail($"the schema name {schema.Name} was taken by other schema");
    }

    public async Task<Schema[]> GetAll(string type, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(ColumnType, type);
        }

        var items = await kateQueryExecutor.Many(query, cancellationToken);
        return items.Select(x => CheckResult(ParseSchema(x))).ToArray();
    }

    public async Task<Schema?> GetByIdDefault(int id, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().Where(ColumnId, id);
        var item = await kateQueryExecutor.One(query, cancellationToken);
        var res = ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNamePrefixDefault(string name, string type, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().WhereStarts(ColumnName ,name).Where(ColumnType, type);
        var item = await kateQueryExecutor.One(query, cancellationToken);
        var res = ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNameDefault(string name, string type, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().Where(ColumnName ,name).Where(ColumnType, type);
        var item = await kateQueryExecutor.One(query, cancellationToken);
        var res = ParseSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public object CastToDatabaseType(Attribute attribute, string str)
    {
        return definitionExecutor.CastToDatabaseType(attribute.DataType, str);
    }

    public async Task<Schema> Save(Schema dto, CancellationToken cancellationToken = default)
    {
        CheckResult(await NameNotTakenByOther(dto, cancellationToken));
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeSaveSchema, new SchemaMeta(dto.Id), dto);
        if (exit)
        {
            return dto;
        }

        await SaveSchema(dto, cancellationToken);
        return dto;
    }

    public async Task Delete(int id, CancellationToken cancellationToken = default)
    {
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeDeleteSchema, new SchemaMeta(id), null);
        if (exit)
        {
            return;
        }

        var query = new SqlKata.Query(TableName).Where(ColumnId, id).AsUpdate([ColumnDeleted], [1]);
        await kateQueryExecutor.Exec(query, cancellationToken);
    }

    public async Task EnsureTopMenuBar(CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().Where(ColumnName, SchemaName.TopMenuBar);
        var item = await kateQueryExecutor.One(query, cancellationToken);
        if (item is not null)
        {
            return;
        }

        var menuBarSchema = new Schema
        {
            Name = SchemaName.TopMenuBar,
            Type = SchemaType.Menu,
            Settings = new Settings
            {
                Menu = new Menu
                {
                    Name = SchemaName.TopMenuBar,
                }
            }
        };
        await SaveSchema(menuBarSchema, cancellationToken);
    }

    public async Task EnsureSchemaTable(CancellationToken cancellationToken = default)
    {
        var cols = await definitionExecutor.GetColumnDefinitions(TableName, cancellationToken);
        if (cols.Length > 0)
        {
            return;
        }

        var entity = new Entity
        {
            TableName = TableName,
            Attributes =
            [
                new Attribute
                {
                    Field = ColumnName,
                    DataType = DataType.String,
                },
                new Attribute
                {
                    Field = ColumnType,
                    DataType = DataType.String,
                },
                new Attribute
                {
                    Field = ColumnSettings,
                    DataType = DataType.Text,
                },
                new Attribute
                {
                    Field = ColumnCreatedBy,
                    DataType = DataType.String,
                }
            ]
        };
        entity.EnsureDefaultAttribute();
        entity.EnsureDeleted();
        await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions(), cancellationToken);
    }
    public async Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken cancellationToken)
    {
        var menuBarSchema = NotNull(await GetByNameDefault(SchemaName.TopMenuBar, SchemaType.Menu, cancellationToken))
            .ValOrThrow("not find top menu bar");
        var menuBar = menuBarSchema.Settings.Menu;
        if (menuBar is not null)
        {
            var link = "/entities/" + entity.Name;
            var menuItem = menuBar.MenuItems.FirstOrDefault(me => me.Url == link);
            if (menuItem is null)
            {
                menuBar.MenuItems =
                [
                    ..menuBar.MenuItems, new MenuItem
                    {
                        Icon = "pi-bolt",
                        Url = link,
                        Label = entity.Title
                    }
                ];
            }

            await SaveSchema(menuBarSchema, cancellationToken);
        }
    }

    private const string TableName = "__schemas";
    private const string ColumnId = "id";
    private const string ColumnName = "name";
    private const string ColumnType = "type";
    private const string ColumnSettings = "settings";
    private const string ColumnDeleted = "deleted";
    private const string ColumnCreatedBy = "created_by";

    private static string[] Fields() => [ColumnId, ColumnName, ColumnType, ColumnSettings, ColumnCreatedBy];

    private static SqlKata.Query BaseQuery()
    {
        return new SqlKata.Query(TableName).Select(Fields()).Where(ColumnDeleted, false);
    }

    private async Task SaveSchema(Schema dto, CancellationToken cancellationToken)
    {
        if (dto.Id == 0)
        {
            var record = new Dictionary<string, object>
            {
                { ColumnName, dto.Name },
                { ColumnType, dto.Type },
                { ColumnSettings, JsonSerializer.Serialize(dto.Settings) },
                { ColumnCreatedBy, dto.CreatedBy }
            };
            var query = new SqlKata.Query(TableName).AsInsert(record, true);
            dto.Id = await kateQueryExecutor.Exec(query, cancellationToken);
        }
        else
        {
            var query = new SqlKata.Query(TableName)
                .Where(ColumnId, dto.Id)
                .AsUpdate(
                    [ColumnName, ColumnType, ColumnSettings],
                    [dto.Name, dto.Type, JsonSerializer.Serialize(dto.Settings)]
                );
            await kateQueryExecutor.Exec(query, cancellationToken);
        }
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
            var s = JsonSerializer.Deserialize<Settings>((string)record[ColumnSettings]);
            return new Schema
            {
                Name = (string)record[ColumnName],
                Type = (string)record[ColumnType],
                Settings = s!,
                CreatedBy = (string)record[ColumnCreatedBy],
                Id = record[ColumnId] switch
                {
                    int val => val,
                    long val => (int)val,
                    _ => 0
                }
            };

        });
    }
}
