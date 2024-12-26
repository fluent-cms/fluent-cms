using System.Data;
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.RelationDbDao;

public class PostgresDao(ILogger<PostgresDao> logger, NpgsqlConnection connection):IDao
{
    private readonly Compiler _compiler = new PostgresCompiler();
    public async ValueTask<IDbTransaction> BeginTransaction() => await connection.BeginTransactionAsync();

    public bool TryParseDataType(string s, string type, out DatabaseTypeValue? result)
    {
        result = type switch
        {
            DataType.String or DataType.Text or DataType.Na => new DatabaseTypeValue(s),
            DataType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            DataType.Datetime when DateTime.TryParse(s, out var resultDateTime) => new DatabaseTypeValue(D: resultDateTime),
            _ => null
        };
        return result != null;
    }

    public Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return queryFunc(db);
    }

    public async Task CreateTable(string table, ColumnDefinition[] cols,CancellationToken ct,IDbTransaction? tx)
    {
        var parts = cols.Select(column => column.ColumnName.ToLower() switch
        {
            "id" => "id SERIAL PRIMARY KEY",
            "deleted" => "deleted BOOLEAN DEFAULT FALSE",
            "created_at" => "created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            "updated_at" => "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            _ => $"\"{column.ColumnName}\" {DataTypeToString(column.DataType)}"
        });
        
        var sql= $"CREATE TABLE {table} ({string.Join(", ", parts)});";
        sql += $"""
                CREATE OR REPLACE FUNCTION __update_updated_at_column()
                    RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = NOW();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                
                CREATE TRIGGER update_{table}_updated_at 
                                BEFORE UPDATE ON {table} 
                                FOR EACH ROW
                EXECUTE FUNCTION __update_updated_at_column();
                """;
        await ExecuteQuery(sql, tx, cmd => cmd.ExecuteNonQueryAsync(ct));
    }

    public async Task AddColumns(string table, ColumnDefinition[] cols, CancellationToken ct,IDbTransaction? tx)
    {
        var parts = cols.Select(x =>
            $"Alter Table {table} ADD COLUMN \"{x.ColumnName}\" {DataTypeToString(x.DataType)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await ExecuteQuery(sql, tx, cmd => cmd.ExecuteNonQueryAsync(ct));
    }
    
    public async Task<ColumnDefinition[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = @"SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_name = @tableName;";

        return await ExecuteQuery(sql, null,async command =>
        {
            await using var reader = command.ExecuteReader();
            var columnDefinitions = new List<ColumnDefinition>();
            while (await reader.ReadAsync(ct))
            {
                columnDefinitions.Add(new ColumnDefinition(reader.GetString(0),reader.GetString(1)));
            }
            return columnDefinitions.ToArray();
        }, ("tableName", table));
    }

    private string DataTypeToString(string dataType)
    {
        return dataType switch
        {
            DataType.Int => "INTEGER",
            DataType.Text => "TEXT",
            DataType.Datetime => "TIMESTAMP",
            DataType.String => "varchar(255)",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    //use callback  instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(string sql, IDbTransaction? tx, Func<NpgsqlCommand, Task<T>> executeFunc, params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (tx is not null)
            command.Transaction = tx as NpgsqlTransaction ?? throw new Exception("Transaction not supported");
       
        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
}