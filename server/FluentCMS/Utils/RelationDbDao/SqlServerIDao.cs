using System.Data;
using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.RelationDbDao;

public class SqlServerIDao(SqlConnection connection, ILogger<SqlServerIDao> logger ) : IDao
{
    private readonly Compiler _compiler = new SqlServerCompiler();
    private  SqlTransaction? _transaction = null;

    public async Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db,_transaction);
    }

    public async ValueTask<IDbTransaction> BeginTransaction()
    {
        _transaction= await connection.BeginTransactionAsync() as SqlTransaction;
        return _transaction!;
    }

    public void EndTransaction() => _transaction = null;

    public bool TryParseDataType(string s, string type, out DatabaseTypeValue? result)
    {
        result = type switch
        {
            ColumnType.Datetime or ColumnType.String or ColumnType.Text => new DatabaseTypeValue(s),
            ColumnType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            _ => null
        };
        return result != null;
    }

    public async Task CreateTable(string table, IEnumerable<Column> cols,  CancellationToken ct)
    {
        var columnDefinitionStrs = cols.Select(column => column.Name.ToLower() switch
        {
            "id" => "[id] INT IDENTITY(1,1) PRIMARY KEY",
            "deleted" => "[deleted] BIT DEFAULT 0",
            "created_at" => "[created_at] DATETIME DEFAULT GETDATE()",
            "updated_at" => "[updated_at] DATETIME DEFAULT GETDATE()",
            _ => $"[{column.Name}] {DataTypeToString(column.Type)}"
        });

        var sql = $"CREATE TABLE [{table}] ({string.Join(", ", columnDefinitionStrs)});";
        
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
        sql = $"""
               CREATE TRIGGER trg_{table}_updated_at 
               ON [{table}] 
               AFTER UPDATE
               AS 
               BEGIN
                   SET NOCOUNT ON;
                   UPDATE [{table}]
                   SET [updated_at] = GETDATE()
                   FROM inserted i
                   WHERE [{table}].[id] = i.[id];
               END;
               """;

        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"ALTER TABLE [{table}] ADD [{x.Name}] {DataTypeToString(x.Type)}"
        );
        var sql = string.Join(";", parts);
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(ct));
    }

    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = @"
                SELECT COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName";

        return await ExecuteQuery(sql, async command =>
        {
            var columnDefinitions = new List<Column>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                columnDefinitions.Add(new Column
                ( 
                    Name : reader.GetString(0),
                    Type : StringToDataType(reader.GetString(1))
                ));
            }

            return columnDefinitions.ToArray();
        }, ("tableName", table));
    }

    private string DataTypeToString(string dataType)
    {
        return dataType switch
        {
            ColumnType.Int => "INT",
            ColumnType.Text => "TEXT",
            ColumnType.Datetime => "DATETIME",
            ColumnType.String => "NVARCHAR(255)",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    private string StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "int" => ColumnType.Int,
            "text" => ColumnType.Text,
            "datetime" => ColumnType.Datetime,
            _ => ColumnType.String
        };
    }

    // Use callback instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(
        string sql, 
        Func<SqlCommand, Task<T>> executeFunc, 
        params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = new SqlCommand(sql, connection);
        command.Transaction = _transaction as SqlTransaction;

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
}