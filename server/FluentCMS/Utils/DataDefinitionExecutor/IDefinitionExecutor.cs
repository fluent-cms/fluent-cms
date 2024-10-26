namespace FluentCMS.Utils.DataDefinitionExecutor;

public delegate object CastDelegate(string s, string dbType ); 

public interface IDefinitionExecutor
{
    CastDelegate GetCastDelegate();
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken cancellationToken);
}