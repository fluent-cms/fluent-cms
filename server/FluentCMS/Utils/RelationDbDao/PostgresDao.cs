using System.Data;
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.RelationDbDao;

public class PostgresDao(ILogger<PostgresDao> logger, NpgsqlConnection connection):IDao
{
    private NpgsqlTransaction? _transaction;
    private readonly Compiler _compiler = new PostgresCompiler();

    public async ValueTask<IDbTransaction> BeginTransaction()
    {
        _transaction = await connection.BeginTransactionAsync();   
        return _transaction!;
    }

    public void EndTransaction()=> _transaction = null;

    public bool TryParseDataType(string s, string type, out DatabaseTypeValue? result)
    {
        result = type switch
        {
            ColumnType.String or ColumnType.Text  => new DatabaseTypeValue(s),
            ColumnType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            ColumnType.Datetime when DateTime.TryParse(s, out var resultDateTime) => new DatabaseTypeValue(D: resultDateTime),
            _ => null
        };
        return result != null;
    }

    public Task<T> ExecuteKateQuery<T>(Func<QueryFactory, IDbTransaction?, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
            
        return queryFunc(db,_transaction);
    }

    public async Task CreateTable(string table, IEnumerable<Column> cols,CancellationToken ct)
    {
        var parts = cols.Select(column => column.Name.ToLower() switch
        {
            "id" => "id SERIAL PRIMARY KEY",
            "deleted" => "deleted BOOLEAN DEFAULT FALSE",
            "created_at" => "created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            "updated_at" => "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            _ => $"\"{column.Name}\" {DataTypeToString(column.Type)}"
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
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
    }

    public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct)
    {
        var parts = cols.Select(x =>
            $"Alter Table {table} ADD COLUMN \"{x.Name}\" {DataTypeToString(x.Type)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await ExecuteQuery(sql, cmd => cmd.ExecuteNonQueryAsync(ct));
    }
    
    public async Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct)
    {
        var sql = @"SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_name = @tableName;";

        return await ExecuteQuery(sql, async command =>
        {
            await using var reader = command.ExecuteReader();
            var columnDefinitions = new List<Column>();
            while (await reader.ReadAsync(ct))
            {
                columnDefinitions.Add(new Column(reader.GetString(0),reader.GetString(1)));
            }
            return columnDefinitions.ToArray();
        }, ("tableName", table));
    }

    private string DataTypeToString(string dataType)
    {
        return dataType switch
        {
            ColumnType.Int => "INTEGER",
            ColumnType.Text => "TEXT",
            ColumnType.Datetime => "TIMESTAMP",
            ColumnType.String => "varchar(255)",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    //use callback  instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(string sql, Func<NpgsqlCommand, Task<T>> executeFunc, params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction;
       
        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
}