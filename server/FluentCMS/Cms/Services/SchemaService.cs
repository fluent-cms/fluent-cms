using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentResults;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;
using Query = FluentCMS.Utils.QueryBuilder.Query;

namespace FluentCMS.Cms.Services;

using static InvalidParamExceptionFactory;


public sealed partial class SchemaService(
    IDefinitionExecutor definitionExecutor, 
    KateQueryExecutor kateQueryExecutor, 
    HookRegistry hookRegistry, 
    IServiceProvider provider
    ) : ISchemaService
{
    public object CastToDatabaseType(Attribute attribute, string str)
    {
        return definitionExecutor.CastToDatabaseType(attribute.DataType, str);
    }
    
    public async Task<Schema> Save(Schema dto, CancellationToken cancellationToken = default)
    {
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeSaveSchema, new SchemaMeta(dto.Id), dto);
        if (exit)
        {
            return dto;
        }
        return await InternalSave(dto, cancellationToken);
    }


    public async Task<Schema> SaveTableDefine(Schema dto, CancellationToken cancellationToken = default)
    {
        var exit = await hookRegistry.Trigger(provider,Occasion.BeforeSaveSchema, new SchemaMeta(dto.Id), dto);
        if (exit)
        {
            return dto;
        }
        var entity = NotNull(dto.Settings.Entity).ValOrThrow("invalid payload");
        entity.Init();
        entity.EnsureDefaultAttribute();
        var cols = await definitionExecutor.GetColumnDefinitions(entity.TableName,cancellationToken);
        await VerifyEntity(dto, cols, entity,cancellationToken);
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.crosstable))
        {
            await CreateCrosstable(entity, attribute,cancellationToken);
        }

        if (cols.Length > 0) //if table exists, alter table add columns
        {
            var columnDefinitions = entity.AddedColumnDefinitions(cols);
            if (columnDefinitions.Length > 0)
            {
                await definitionExecutor.AlterTableAddColumns(entity.TableName, columnDefinitions,cancellationToken);
            }
        }
        else
        {
            entity.EnsureDeleted();
            await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions(),cancellationToken);
            //no need to expose deleted field to frontend 
            entity.RemoveDeleted();
        }
        await EnsureEntityInTopMenuBar(entity,cancellationToken);
        return await Save(dto,cancellationToken);
    }
    public async Task<bool> Delete(int id, CancellationToken cancellationToken = default)
    {
        var exit = await hookRegistry.Trigger(provider,Occasion.BeforeDeleteSchema,new SchemaMeta(id),null);
        if (exit)
        {
            return false;
        }
        
        var query = new SqlKata.Query(TableName).Where(ColumnId,id).AsUpdate([ColumnDeleted],[1]);
        await kateQueryExecutor.Exec(query,cancellationToken);
        return true;
    }
    
    public async Task<Schema[]> GetAll(string type,CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(ColumnType, type);
        }
        return  ParseSchema(await kateQueryExecutor.Many(query,cancellationToken)); 
    }

    public async Task<Schema?> GetByIdDefault(int id, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().Where(ColumnId, id);
        return ParseSchema(await kateQueryExecutor.One(query, cancellationToken));
    }

    public async Task<Schema?> GetByNameDefault(string name, CancellationToken cancellationToken = default)
    {
        var query =  BaseQuery().Where(ColumnName, name);
        return ParseSchema(await kateQueryExecutor.One(query,cancellationToken));
    }

    public async Task<Schema> GetByIdAndVerify(int id,bool extend, CancellationToken cancellationToken = default)
    {
        var item =  NotNull(await GetByIdDefault(id,cancellationToken)).
            ValOrThrow($"Schema [{id}] does not exist");
        if (extend)
        {
            CheckResult(await InitIfSchemaIsEntity(item, cancellationToken));
        }
        return item;
    }
    
    public async Task<Schema> GetByNameVerify(string name,bool extend, CancellationToken cancellationToken = default)
    {
        var item =  NotNull(await GetByNameDefault(name,cancellationToken)).
            ValOrThrow($"Schema [{name}] does not exist");
        if (extend)
        {
            CheckResult(await InitIfSchemaIsEntity(item, cancellationToken));
        }
        return item;
    }

    public async Task<Query> GetViewByName(string name, CancellationToken cancellationToken = default)
    {
        StrNotEmpty(name).ValOrThrow("view name should not be empty");
        var query = BaseQuery().Where(ColumnName, name).Where(ColumnType, SchemaType.Query);
        var item = ParseSchema(await kateQueryExecutor.One(query,cancellationToken));
        item = NotNull(item).ValOrThrow($"didn't find {name}");
        var view = NotNull(item.Settings.Query)
            .ValOrThrow("invalid view format");
        var entityName = StrNotEmpty(view.EntityName)
            .ValOrThrow($"referencing entity was not set for {view}");
        view.Entity = CheckResult(await GetEntityByNameOrDefault(entityName, cancellationToken));
        CheckResult(await InitViewSelection(view, cancellationToken));
        return view;
    }
    
    public async Task<Result<Entity>> GetEntityByNameOrDefault(string name, CancellationToken cancellationToken = default)
    {
        return await GetEntityByNameOrDefault(name, true,cancellationToken);
    }

    public async Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken = default)
    {
        StrNotEmpty(tableName).ValOrThrow("Table name should not be empty");
        var entity = new Entity { TableName = tableName };
        entity.LoadAttributesByColDefines(await definitionExecutor.GetColumnDefinitions(tableName,cancellationToken));
        return entity;
    }

    public async Task EnsureTopMenuBar(CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().Where(ColumnName, SchemaName.TopMenuBar);
        var item = ParseSchema(await kateQueryExecutor.One(query,cancellationToken));
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
        await SaveSchema(menuBarSchema,cancellationToken);
    }

    public async Task EnsureSchemaTable(CancellationToken cancellationToken = default)
    {
        var cols = await definitionExecutor.GetColumnDefinitions(TableName,cancellationToken);
        if (cols.Length > 0)
        {
            return;
        }
        var entity = new Entity
        {
            TableName = TableName,
            Attributes = [
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
        await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions(),cancellationToken);
    }

    public async Task<Schema> AddOrSaveEntity(Entity entity, CancellationToken cancellationToken)
    {
        var find = await GetByNameDefault(entity.Name, cancellationToken);
        var schema = new Schema
        {
            Name = entity.Name,
            Type = SchemaType.Entity,
            Settings = new Settings
            {
                Entity = entity
            }
        };
        
        if (find is not null)
        {
            True(find.Type == SchemaType.Entity).
                ThrowNotTrue("Schema Name exists and it's not entity");
            schema.Id = find.Id;
        }
        return await SaveTableDefine(schema,cancellationToken);
    }
    
    public async Task<Schema> AddOrSaveSimpleEntity(string entityName, string field, string? lookup, string? crossTable, CancellationToken cancellationToken)
    {
        var entity = new Entity
        {
            Name = entityName,
            TableName = entityName,
            Title = entityName,
            DefaultPageSize = 10,
            PrimaryKey = "id",
            TitleAttribute = field,
            Attributes =
            [
                new Attribute
                {
                    Field = field,
                    Header = field,
                    InList = true,
                    InDetail = true,
                    DataType = DataType.String
                }
            ]
        };
        if (!string.IsNullOrWhiteSpace(lookup))
        {
            entity.Attributes = entity.Attributes.Append(new Attribute
            {
                Field = lookup,
                Options = lookup,
                Header = lookup,
                InList = true,
                InDetail = true,
                DataType = DataType.Int,
                Type = DisplayType.lookup,
            }).ToArray();

        }

        if (!string.IsNullOrWhiteSpace(crossTable))
        {
            entity.Attributes = entity.Attributes.Append(new Attribute
            {
                Field = crossTable,
                Options = crossTable,
                Header = crossTable,
                DataType = DataType.Na,
                Type = DisplayType.crosstable,
                InDetail = true,
            }).ToArray();
        }
        return await AddOrSaveEntity(entity, cancellationToken);
    }

}
