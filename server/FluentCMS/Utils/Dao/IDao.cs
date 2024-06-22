using SqlKata;

namespace FluentCMS.Utils.Dao;

using Record = IDictionary<string,object>;

public interface IDao
{
    Task<string> GetPrimaryKeyColumn(string tableName);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
    Task<Record[]?> Many(Query query);
    Task<Record?> One(Query? query);
    Task<int> Count(Query query);
    Task<int?> Exec(Query? query);
}