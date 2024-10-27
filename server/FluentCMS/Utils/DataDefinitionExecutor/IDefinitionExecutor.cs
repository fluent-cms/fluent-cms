namespace FluentCMS.Utils.DataDefinitionExecutor;


public interface IDefinitionExecutor
{
    object Cast(string s, string type);
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken cancellationToken);
}