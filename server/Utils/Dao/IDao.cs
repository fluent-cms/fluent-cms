using SqlKata;

namespace Utils.Dao;


public interface IDao
{
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions);
    Task AddColumns(string tableName, ColumnDefinition[] columnDefinitions);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
    Task<Record[]?> Many(Query? query);
    Task<Record?> One(Query? query);
    Task<int> Count(Query? query);
    Task<int?> Exec(Query? query);
}