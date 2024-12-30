namespace FluentCMS.Utils.QueryBuilder;

public record Lookup(LoadedEntity SourceEntity, LoadedEntity TargetEntity);

public static class LookupHelper
{
    public static SqlKata.Query LookupTitleQuery(this Lookup lookup, IEnumerable<ValidValue> ids)
    {
        var e = lookup.TargetEntity;
        return lookup.TargetEntity.Basic().Select(e.PrimaryKey, e.TitleAttribute)
            .WhereIn(e.PrimaryKey, ids.GetValues());
    }
}