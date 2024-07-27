using FluentCMS.Models;
using FluentResults;
using SqlKata;
using Utils.DataDefinitionExecutor;
using Utils.KateQueryExecutor;
using Utils.QueryBuilder;
using Attribute = Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;

using System.Threading.Tasks;

public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string View = "view";
}

public static class SchemaName
{
    public const string TopMenuBar = "top-menu-bar";
}
public partial class SchemaService(IDefinitionExecutor definitionExecutor, KateQueryExecutor kateQueryExecutor) : ISchemaService
{
    private const string SchemaTableName = "__schemas";
    private const string SchemaColumnId = "id";
    private const string SchemaColumnName = "name";
    private const string SchemaColumnType = "type";
    private const string SchemaColumnSettings = "settings";
    private const string SchemaColumnDeleted = "deleted";

    private static string[] Fields() => [SchemaColumnId, SchemaColumnName, SchemaColumnType, SchemaColumnSettings];

    private static Query BaseQuery()
    {
        return new Query(SchemaTableName).Select(Fields()).Where(SchemaColumnDeleted, false);
    }

    
    public async Task<Schema[]> GetAll(string type)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(SchemaColumnType, type);
        }
        return  ParseSchema(await kateQueryExecutor.Many(query)); 
    }

    public async Task<Schema?> GetByIdOrNameDefault(string name)
    {
        var query = int.TryParse(name, out var id)
            ? BaseQuery().Where(SchemaColumnId, id)
            : BaseQuery().Where(SchemaColumnName, name);
        return ParseSchema(await kateQueryExecutor.One(query));
    }

    public async Task<Schema> GetByIdOrName(string name,bool extend)
    {
        var item =  Val.NotNull(await GetByIdOrNameDefault(name)).
            ValOrThrow($"Schema [{name}] does not exist");
        if (extend)
        {
            Val.CheckResult(await InitIfSchemaIsEntity(item));
        }
        return item;
    }

    public async Task<Schema> Save(Schema dto)
    {
        var query = BaseQuery().Where(SchemaColumnName, dto.Name)
            .WhereNot(SchemaColumnId, dto.Id);
        var existing = await kateQueryExecutor.Count(query);
        Val.CheckBool(existing ==0)
            .ThrowFalse($"the schema name {dto.Name} exists");
        await VerifyIfSchemaIsView(dto);
        await SaveSchema(dto);
        return dto;
    }

    public async Task<View> GetViewByName(string name)
    {
        Val.StrNotEmpty(name).ValOrThrow("view name should not be empty");
        var query = BaseQuery().Where(SchemaColumnName, name).Where(SchemaColumnType, SchemaType.View);
        var item = ParseSchema(await kateQueryExecutor.One(query));
        item = Val.NotNull(item).ValOrThrow($"didn't find {name}");
        var view = Val.NotNull(item.Settings.View)
            .ValOrThrow("invalid view format");
        var entityName = Val.StrNotEmpty(view.EntityName)
            .ValOrThrow($"referencing entity was not set for {view}");
        view.Entity = Val.CheckResult(await GetEntityByNameOrDefault(entityName));
        return view;
    }

    public async Task<Result<Entity>> GetEntityByNameOrDefault(string name)
    {
        return await GetEntityByNameOrDefault(name, true);
    }

    public async Task<Schema> SaveTableDefine(Schema dto)
    {
        var entity = Val.NotNull(dto.Settings?.Entity).ValOrThrow("invalid payload");
        var cols = await definitionExecutor.GetColumnDefinitions(entity.TableName);
        await VerifyEntity(dto, cols, entity);
        
        foreach (var attribute in entity.GetAttributesByType(DisplayType.crosstable))
        {
            await CreateCrosstable(entity, attribute);
        }

        if (cols.Length > 0) //if table exists, alter table add columns
        {
            await definitionExecutor.AlterTableAddColumns(entity.TableName, entity.AddedColumnDefinitions(cols));
        }
        else
        {
            entity.EnsureDefaultAttribute();
            await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions());
            //no need to expose deleted field to frontend 
            entity.RemoveDeleted();
        }

        await EnsureEntityInTopMenuBar(entity);
        return await Save(dto);
    }


    public async Task<Entity?> GetTableDefine(string tableName)
    {
        Val.StrNotEmpty(tableName).ValOrThrow("Table name should not be empty");
        var entity = new Entity { TableName = tableName };
        entity.LoadAttributesByColDefines(await definitionExecutor.GetColumnDefinitions(tableName));
        return entity;
    }

    public async Task<bool> Delete(int id)
    {
        var query = new Query(SchemaTableName).Where(SchemaColumnId,id).AsUpdate([SchemaColumnDeleted],[1]);
        await kateQueryExecutor.Exec(query);
        return true;
    }

    public async Task AddTopMenuBar()
    {
        var query = BaseQuery().Where(SchemaColumnName, SchemaName.TopMenuBar);
        var item = ParseSchema(await kateQueryExecutor.One(query));
        if (item is not null)
        {
            return;
        }
        
        var menuItem = new MenuItem
        {
            Label = "Schema Builder",
            Url = "/schema-ui/list.html",
            Icon = "pi-cog",
            IsHref = true
        };
        var menuBarSchema = new Schema
        {
            Name = SchemaName.TopMenuBar,
            Type = SchemaType.Menu,
        };
        menuBarSchema.Settings = new Settings
        {
            Menu = new Menu
            {
                Name = SchemaName.TopMenuBar,
                MenuItems = [menuItem]
            }
        };
        await SaveSchema(menuBarSchema);
    }

    public async Task AddSchemaTable()
    {
        var cols = await definitionExecutor.GetColumnDefinitions(SchemaTableName);
        if (cols.Length > 0)
        {
            return;
        }
        var entity = new Entity
        {
            TableName = SchemaTableName,
            Attributes = [
                new Attribute
                {
                    Field = SchemaColumnName,
                    DataType = DataType.String,
                },
                new Attribute
                {
                    Field = SchemaColumnType,
                    DataType = DataType.String,
                }, 
                new Attribute
                {
                    Field = SchemaColumnSettings,
                    DataType = DataType.Text,
                },            
            ]
        };
        entity.EnsureDefaultAttribute();
        await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions());
    }
}
