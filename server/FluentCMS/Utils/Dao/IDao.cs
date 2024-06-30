using SqlKata;

namespace FluentCMS.Utils.Dao;


public interface IDao
{
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
    Task<Record[]?> Many(Query? query);
    Task<Record?> One(Query? query);
    Task<int> Count(Query? query);
    Task<int?> Exec(Query? query);
}