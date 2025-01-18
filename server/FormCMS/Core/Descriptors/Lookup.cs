namespace FormCMS.Core.Descriptors;

public record Lookup(LoadedEntity TargetEntity);

public static class LookupHelper
{
    public static SqlKata.Query LookupTitleQuery(this Lookup lookup, IEnumerable<ValidValue> ids)
    {
        var e = lookup.TargetEntity;
        return lookup.TargetEntity.Basic().Select(e.PrimaryKey, e.LabelAttributeName)
            .WhereIn(e.PrimaryKey, ids.GetValues());
    }
}