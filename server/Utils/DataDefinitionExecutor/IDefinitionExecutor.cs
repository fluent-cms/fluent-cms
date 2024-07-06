namespace Utils.DataDefinitionExecutor;

public interface IDefinitionExecutor
{
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions);
    Task AddColumns(string tableName, ColumnDefinition[] columnDefinitions);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
}