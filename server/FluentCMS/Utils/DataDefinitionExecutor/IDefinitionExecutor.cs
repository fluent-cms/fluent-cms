namespace FluentCMS.Utils.DataDefinitionExecutor;

public interface IDefinitionExecutor
{
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
}