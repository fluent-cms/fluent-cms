using SqlKata;

namespace FluentCMS.Utils.Dao;


public interface IDao
{
    Task<string> GetPrimaryKeyColumn(string tableName);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
    Task<IDictionary<string,object>[]?> Many(Query? query);
    Task<IDictionary<string,object>?> One(Query? query);
    Task<int> Count(Query? query);
    Task<int?> Exec(Query? query);
}