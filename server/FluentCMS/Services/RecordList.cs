namespace FluentCMS.Services;

public struct RecordList
 {
     public IDictionary<string,object>[]? Items { get; set; }
     public int TotalRecords { get; set; }
     public string Cursor { get; set; }
     public string HasMore { get; set; }
 }