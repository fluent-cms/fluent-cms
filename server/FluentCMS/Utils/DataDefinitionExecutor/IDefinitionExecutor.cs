namespace FluentCMS.Utils.DataDefinitionExecutor;


public interface IDefinitionExecutor
{
    bool CastToDatabaseDataType(string s, string type, out object? data);
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken cancellationToken);
}