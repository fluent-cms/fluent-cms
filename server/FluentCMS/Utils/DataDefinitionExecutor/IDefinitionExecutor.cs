namespace FluentCMS.Utils.DataDefinitionExecutor;

public interface IDefinitionExecutor
{
    public object CastToDatabaseType(DataType dataType, string str);
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken cancellationToken);
}