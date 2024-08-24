using MongoDB.Bson;

namespace FluentCMS.Utils.Nosql;

public static class BsonDocumentExt
{
    public static IList<IDictionary<string, object>> ToRecords(this IList<BsonDocument> bsonDocuments)
    {
        return bsonDocuments.Select(x => x.ToRecord()).ToList();
    }
    
    public static IDictionary<string, object> ToRecord(this BsonDocument bsonDocument)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var element in bsonDocument.Elements)
        {
            dictionary[element.Name] = ConvertBsonValue(element.Value);
        }

        return dictionary;
    }

    private static object ConvertBsonValue(BsonValue bsonValue)
    {
        return (bsonValue.BsonType switch
        {
            BsonType.Document => ((BsonDocument)bsonValue).ToDictionary(),
            BsonType.Array => ConvertBsonArray(bsonValue.AsBsonArray),
            BsonType.Boolean => bsonValue.AsBoolean,
            BsonType.DateTime => bsonValue.ToUniversalTime(),
            BsonType.Double => bsonValue.AsDouble,
            BsonType.Int32 => bsonValue.AsInt32,
            BsonType.Int64 => bsonValue.AsInt64,
            BsonType.String => bsonValue.AsString,
            BsonType.Null => null,
            _ => bsonValue.ToString() // Fallback to string representation for unsupported types
        })!;
    }

    private static List<object> ConvertBsonArray(BsonArray bsonArray)
    {
        var list = new List<object>();
        foreach (var item in bsonArray)
        {
            list.Add(ConvertBsonValue(item));
        }
        return list;
    }
}