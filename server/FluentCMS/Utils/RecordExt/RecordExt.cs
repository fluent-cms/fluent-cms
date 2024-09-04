namespace FluentCMS.Utils.RecordExt;

public static class RecordExt
{
    public static object Value(this Record record, string field)
    {
        var arr = field.Split(".");
        object current = record;
        foreach (var part in arr)
        {
            if (current is Record currentRecord && currentRecord.TryGetValue(part, out var next))
            {
                current = next;
            }
            else
            {
                // If the field is not found or the type is incorrect, return an empty string or handle error
                return string.Empty;
            }
        }

        return current;
    }
}