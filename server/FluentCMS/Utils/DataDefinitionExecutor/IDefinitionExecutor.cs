namespace FluentCMS.Utils.DataDefinitionExecutor;

public interface IDefinitionExecutor
{
    public object CastToDatabaseType(DataType dataType, string str);
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
}