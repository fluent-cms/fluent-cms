namespace FluentCMS.Utils.DataDefinitionExecutor;


public sealed record DatabaseTypeValue(string S = "", int? I = null, DateTime? D = null);
public record DefinitionExecutorOptions(string ConnectionString);
public interface IDefinitionExecutor
{
    bool TryParseDataType(string s, string type, out DatabaseTypeValue? data);
    Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken ct);
}