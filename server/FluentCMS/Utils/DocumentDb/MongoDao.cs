using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.DocumentDb;

public sealed class MongoDao(ILogger<MongoDao> logger, IMongoDatabase db):IDocumentDbDao 
{
    private static readonly FilterDefinitionBuilder<BsonDocument> Filter =  Builders<BsonDocument>.Filter;
    private IMongoCollection<BsonDocument> GetCollection(string name) => db.GetCollection<BsonDocument>(name);
    public Task Delete(string collection, string id) 
        => GetCollection(collection).DeleteOneAsync(Filter.Eq("id", id));

    public Task Upsert(string collection, string id, Record item)
        => GetCollection(collection).ReplaceOneAsync(
            Filter.Eq("id", id),
            item.ToBsonDocument(),
            new ReplaceOptions { IsUpsert = true });

    public Task BatchInsert(string collection, IEnumerable<Record> items)
        => GetCollection(collection).InsertManyAsync(items.Select(x => x.ToBsonDocument()));

    public async Task<Result<Record[]>> Query(
        string collection, 
        IEnumerable<ValidFilter> validFilters, 
        ValidSort[] validSorts, 
        ValidPagination pagination,
        ValidSpan? span)
    {
        logger.LogInformation("Querying for {collection}", collection);
        if (!validFilters.ToFilter().Try(out var filters, out var err))
            return Result.Fail(err);

        if (span is not null)
        {
            if (!span.GetFilters([..validSorts]).Try(out var spanFilters, out err))
                return Result.Fail(err);
            filters = [..filters, spanFilters];
        }

        var records = new List<Record>();
        await GetCollection(collection)
            .Find(Filter.And(filters))
            .Sort(validSorts.ToSort())
            .Skip(pagination.Offset)
            .Limit(pagination.Limit)
            .ForEachAsync(x => records.Add(x.ToRecord()));
        
        return records.ToArray();
    }
}