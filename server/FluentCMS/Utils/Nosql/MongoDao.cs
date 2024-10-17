using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public record MongoConfig(string ConnectionString, string DatabaseName);

public sealed class MongoDao:INosqlDao 
{
    private readonly IMongoDatabase _mongoDatabase ;
    private readonly ILogger<MongoDao> _logger ;

    public MongoDao(MongoConfig config, ILogger<MongoDao> logger)
    {
        var client = new MongoClient(config.ConnectionString);
        _mongoDatabase = client.GetDatabase(config.DatabaseName);
        _logger = logger;

    }
    public async Task Delete(string collectionName, string id)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("id", id);
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        await collection.DeleteOneAsync(filter);
        _logger.LogInformation($"Deleted document with filter: {filter}");
    }
    
    public async Task Upsert(string collectionName, string id, Record item)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("id", id);
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        await collection.ReplaceOneAsync(filter, new BsonDocument(item), new ReplaceOptions{IsUpsert = true});
        _logger.LogInformation($"Replaced document with filter: {filter}");
    }
    
    public async Task BatchInsert(string collectionName, IEnumerable<Record> items)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var docs = items.Select(x => new BsonDocument(x));
        await collection.InsertManyAsync(docs);
        _logger.LogInformation($"Inserted {docs.Count()} documents");
    }

    public async Task<Result<Record[]>> Query(string collectionName, IEnumerable<ValidFilter> filters, ValidPagination pagination,ImmutableArray<Sort>? sorts, ValidCursor? cursor)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var filterRes = MongoExt.GetFiltersDefinition(filters);
        if (filterRes.IsFailed)
        {
            return Result.Fail(filterRes.Errors);
        }
        var filterDefinitions = filterRes.Value;

        if (cursor is not null && sorts is not null)
        {
            var cursorRes = MongoExt.GetCursorFilters(cursor, sorts.Value);
            if (cursorRes.IsFailed)
            {
                return Result.Fail(cursorRes.Errors);
            }
            filterDefinitions.Add(cursorRes.Value);
        }
        var query = collection.Find(Builders<BsonDocument>.Filter.And(filterDefinitions));
        if (sorts?.Count() > 0)
        {
            var sd = MongoExt.GetSortDefinition<BsonDocument>(sorts.Value);
            query = query.Sort(sd);
        }

        if (pagination?.Offset > 0)
        {
            query = query.Skip(pagination.Offset);
        }

        if (pagination?.Limit > 0)
        {
            query = query.Limit(pagination.Limit);
        }
        
        _logger.LogInformation(query.ToString());
        var res = await query.ToListAsync();
        return res.ToRecords().ToArray();
    }
}