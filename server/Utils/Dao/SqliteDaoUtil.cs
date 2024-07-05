using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Utils.Dao;

public class SqliteDaoUtil(string connectionString, bool isDebug)
{
    private Compiler _compiler = new SqliteCompiler();

    internal async Task<T> ExecuteKateQuery<T>(Query? query , Func<QueryFactory, Task<T>> queryFunc)
    {
        Log(query);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        return await queryFunc(db);
    }

    public async Task<T> ExecuteQuery<T>(string sql, Func<SqliteCommand, Task<T>> executeFunc,
        params (string, object)[] parameters)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(sql, connection);

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }

    internal string[] GenerateAddColumnSql(string tableName, ColumnDefinition[] columnDefinitions)
    {
        return columnDefinitions.
            Select(x => $"Alter Table {tableName} ADD COLUMN {x.ColumnName} {GetSqlType(x)} ")
            .ToArray();
    }

    internal string GenerateCreateTableSql(string tableName, ColumnDefinition[] columns)
    {
        var columnDefinitionStrs = columns.Select(column => column.ColumnName.ToLower() switch
        {
            "id" => "id INTEGER  primary key autoincrement",
            "deleted" => "deleted INTEGER   default 0",
            "created_at" => "created_at integer default (datetime('now','localtime'))",
            "updated_at" => "updated_at integer default (datetime('now','localtime'))",
            _ => $"{column.ColumnName} {GetSqlType(column)}"
        });
        var ret= $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitionStrs)});";
        ret += $@"CREATE TRIGGER update_{tableName}_updated_at BEFORE UPDATE ON {tableName} FOR EACH ROW
            BEGIN UPDATE {tableName} SET updated_at = (datetime('now','localtime')) WHERE id = OLD.id; 
            END;";
        return ret;
    }

    private string GetSqlType(ColumnDefinition column)
    {
        return column.DataType switch
        {
            DatabaseType.Int => "INTEGER",
            DatabaseType.Text => "TEXT",
            DatabaseType.Datetime => "INTEGER",
            DatabaseType.String => "TEXT",
            _ => throw new NotSupportedException($"Type {column.DataType} is not supported")
        };
    }

    internal ColumnDefinition CreateColumnDefinition(string columnName, string dataType)
    {
        dataType = dataType.ToLower();
        return new ColumnDefinition
        {
            ColumnName = columnName,
            DataType = dataType switch
            {
                "integer" => DatabaseType.Int,
                "text" => DatabaseType.Text,
                "datetime" => DatabaseType.Text,
                _ when dataType.StartsWith("varchar") => DatabaseType.String,
                _ => DatabaseType.Text
            },
        };
    }

    private void Log(Query? query)
    {
        if (!isDebug || query is null)
        {
            return;
        }

        var res = _compiler.Compile(query);
        Console.WriteLine(res);
    }
}