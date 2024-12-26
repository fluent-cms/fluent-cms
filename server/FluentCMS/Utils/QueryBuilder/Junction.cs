using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Utils.KateQueryExt;

namespace FluentCMS.Utils.QueryBuilder;

public record Junction(
    LoadedEntity JunctionEntity,
    LoadedEntity TargetEntity,
    LoadedEntity SourceEntity,
    
    LoadedAttribute SourceAttribute,
    LoadedAttribute TargetAttribute
);

public static class JunctionHelper
{
    public static ColumnDefinition[] GetColumnDefinitions(this Junction c)
    {
        return
        [
            new ColumnDefinition(DefaultFields.Id, DataType.Int),
            new ColumnDefinition(c.SourceAttribute.Field, DataType.Int),
            new ColumnDefinition(c.TargetAttribute.Field, DataType.Int),
            new ColumnDefinition(DefaultFields.CreatedAt, DataType.Datetime),
            new ColumnDefinition(DefaultFields.UpdatedAt, DataType.Datetime),
            new ColumnDefinition(DefaultFields.Deleted, DataType.Int),
        ];
    }

    public static Junction Junction(LoadedEntity sourceEntity, LoadedEntity targetEntity,
        LoadedAttribute crossAttribute)
    {
        var tableName = GetJunctionTableName(sourceEntity.Name, targetEntity.Name);
        var id = new LoadedAttribute(tableName, DefaultFields.Id);
        var deleted = new LoadedAttribute(tableName, DefaultFields.Deleted);
        sourceEntity = sourceEntity with
        {
            Attributes =
            [..sourceEntity.Attributes.Select(x => x.Field == crossAttribute.Field ? x with { Junction = null } : x)]
        };

        var sourceAttribute = new LoadedAttribute
        (
            Field: $"{sourceEntity.Name}_id",
            TableName: tableName,
            DataType: DataType.Int
        );

        var targetAttribute = new LoadedAttribute
        (
            Field: $"{targetEntity.Name}_id",
            TableName: tableName,
            DataType: DataType.Int
        );

        var crossEntity = new LoadedEntity
        (
            Attributes: [sourceAttribute, targetAttribute],
            PrimaryKeyAttribute: id,
            LoadedTitleAttribute: id,
            DeletedAttribute: deleted,
            Name: tableName,
            TableName: tableName
        );
        return new Junction(
            JunctionEntity: crossEntity,
            TargetEntity: targetEntity,
            SourceEntity: sourceEntity,
            SourceAttribute: sourceAttribute,
            TargetAttribute: targetAttribute
        );
    }

    public static string GetJunctionTableName(string sourceName, string targetName)
    {
        string[] arr = [sourceName, targetName];
        var sorted = arr.OrderBy(x => x).ToArray();

        return $"{sorted[0]}_{sorted[1]}";
    }

    public static SqlKata.Query Delete(this Junction c, ValidValue id, Record[] targetItems)
    {
        var vals = targetItems.Select(x => x[c.TargetEntity.PrimaryKey]);
        return new SqlKata.Query(c.JunctionEntity.TableName).Where(c.SourceAttribute.Field, id.Value)
            .WhereIn(c.TargetAttribute.Field, vals).AsUpdate([c.JunctionEntity.DeletedAttribute.Field], [true]);
    }

    public static SqlKata.Query Insert(this Junction c, ValidValue id, Record[] targetItems)
    {
        string[] cols = [c.SourceAttribute.Field, c.TargetAttribute.Field];
        var vals = targetItems.Select(x => { return new[] { id.Value, x[c.TargetEntity.PrimaryKey] }; });

        return new SqlKata.Query(c.JunctionEntity.TableName).AsInsert(cols, vals);
    }

    public static SqlKata.Query GetNotRelatedItems(
        this Junction c,
        IEnumerable<LoadedAttribute> selectAttributes,
        IEnumerable<ValidFilter> filters,
        IEnumerable<ValidSort> sorts,
        ValidPagination pagination,
        IEnumerable<ValidValue> sourceIds)
    {
        var baseQuery = c.TargetEntity.Basic().Select(selectAttributes.Select(x => x.AddTableModifier()));
        c.ApplyNotRelatedFilter(baseQuery, sourceIds);
        baseQuery.ApplyPagination(pagination);
        baseQuery.ApplyFilters(filters);
        baseQuery.ApplySorts(sorts);
        return baseQuery;
    }


    public static SqlKata.Query GetRelatedItems(
        this Junction c,
        IEnumerable<LoadedAttribute> selectAttributes,
        IEnumerable<ValidFilter> filters,
        ValidSort[] sorts,
        ValidSpan? span,
        ValidPagination? pagination,
        IEnumerable<ValidValue> sourceIds)
    {
        List<LoadedAttribute> attrs = [..selectAttributes, c.SourceAttribute];
        var query = c.TargetEntity.Basic().Select(attrs.Select(x => x.AddTableModifier()));
        c.ApplyRelatedFilter(query, sourceIds);
        if (pagination is not null)
        {
            query.ApplyPagination(pagination);
        }
        query.ApplyFilters(filters);
        query.ApplyCursor(span, sorts);
        query.ApplySorts(span?.Span.IsForward() == false ? sorts.ReverseOrder() : sorts);
        return query;
    }

    public static SqlKata.Query GetNotRelatedItemsCount(
        this Junction c,
        IEnumerable<ValidFilter> filters,
        IEnumerable<ValidValue> sourceIds)
    {
        var query = c.TargetEntity.Basic();
        c.ApplyNotRelatedFilter(query, sourceIds);
        query.ApplyFilters(filters);
        return query;
    }

    public static SqlKata.Query GetRelatedItemsCount(
        this Junction c,
        IEnumerable<ValidFilter> filters,
        IEnumerable<ValidValue> sourceIds)
    {
        var query = c.TargetEntity.Basic();
        c.ApplyRelatedFilter(query, sourceIds);
        query.ApplyFilters(filters);
        return query;
    }

    private static void ApplyRelatedFilter(
        this Junction c,
        SqlKata.Query baseQuery,
        IEnumerable<ValidValue> sourceIds)
    {

        var (a, b) = (c.TargetEntity.PrimaryKeyAttribute.AddTableModifier(), c.TargetAttribute.AddTableModifier());
        baseQuery.Join(c.JunctionEntity.TableName, a, b)
            .WhereIn(c.SourceAttribute.AddTableModifier(), sourceIds.GetValues())
            .Where(c.JunctionEntity.DeletedAttribute.AddTableModifier(), false);
    }

    private static void ApplyNotRelatedFilter(
        this Junction c,
        SqlKata.Query baseQuery,
        IEnumerable<ValidValue> sourceIds)
    {
        var (a, b) = (c.TargetEntity.PrimaryKeyAttribute.AddTableModifier(), c.TargetAttribute.AddTableModifier());
        baseQuery.LeftJoin(c.JunctionEntity.TableName,
            j => j.On(a, b)
                .WhereIn(c.SourceAttribute.AddTableModifier(), sourceIds.GetValues())
                .Where(c.JunctionEntity.DeletedAttribute.AddTableModifier(), false)
        ).WhereNull(c.SourceAttribute.AddTableModifier());
    }
}