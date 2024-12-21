using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace FluentCMS.Utils.DocumentDb;

public sealed class MongoDao(ILogger<MongoDao> logger, IMongoDatabase db):IDocumentDbDao 
{
    private static readonly FilterDefinitionBuilder<BsonDocument> Filter =  Builders<BsonDocument>.Filter;
    private IMongoCollection<BsonDocument> GetCollection(string name) => db.GetCollection<BsonDocument>(name);
    public Task Upsert(string collection, string primaryKey, object primaryKeyValue, object document)
    {
        var doc = document.ToBsonDocument();
        return GetCollection(collection).ReplaceOneAsync(
            Filter.Eq(primaryKey, primaryKeyValue),
            doc,
            new ReplaceOptions { IsUpsert = true }
        );
    }

    public Task Delete(string collection, string id) 
        => GetCollection(collection).DeleteOneAsync(Filter.Eq("id", id));

    public Task Upsert(string collection, string primaryKey, string json)
    {
        var doc = BsonDocument.Parse(json);
        return GetCollection(collection).ReplaceOneAsync(
            Filter.Eq(primaryKey, doc[primaryKey]),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public Task BatchInsert(string collection, string rawJson)
    {
        var items = BsonSerializer.Deserialize<BsonArray>(rawJson).Select(x=> x as BsonDocument);
        return GetCollection(collection).InsertManyAsync(items!);
    }

    public async Task<Result<Record[]>> Query(
        string collection, 
        IEnumerable<ValidFilter> validFilters, 
        ValidSort[] validSorts, 
        ValidPagination pagination,
        ValidSpan? span)
    {
        logger.LogInformation("Querying {collection}, filters={filters}, sort by {sort}", collection, validFilters, validSorts);
        if (!validFilters.ToFilter().Try(out var filters, out var err))
            return Result.Fail(err);

        if (span is not null && !span.Span.IsEmpty())
        {
            if (!span.GetFilters([..validSorts]).Try(out var spanFilters, out err))
                return Result.Fail(err);
            filters = [..filters, spanFilters];
        }

        var records = new List<Record>();
        await GetCollection(collection)
            .Find(filters.Length > 0 ? Filter.And(filters):Filter.Empty)
            .Sort(validSorts.ToSort())
            .Skip(pagination.Offset)
            .Limit(pagination.Limit)
            .ForEachAsync(x => records.Add(x.ToRecord()));
        
        return records.ToArray();
    }
}