using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public record MongoConfig(string ConnectionString, string DatabaseName);

public sealed class MongoNosqlDao:INosqlDao 
{
    private readonly IMongoDatabase _mongoDatabase ;

    public MongoNosqlDao(MongoConfig config)
    {
        var client = new MongoClient(config.ConnectionString);
        _mongoDatabase = client.GetDatabase(config.DatabaseName);

    }
    public async Task Delete(string collectionName, string id)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("id", id);
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        await collection.DeleteOneAsync(filter);
    }
    
    public async Task Upsert(string collectionName, string id, Record item)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("id", id);
        item["id"] = id;
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        await collection.ReplaceOneAsync(filter, new BsonDocument(item), new ReplaceOptions{IsUpsert = true});
    }
    
    public async Task BatchInsert(string collectionName, IEnumerable<Record> items)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var docs = items.Select(x => new BsonDocument(x));
        await collection.InsertManyAsync(docs);
    }

    public async Task<Result<Record[]>> Query(string collectionName, Filters filters, Sorts sorts, Cursor cursor )
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var filterRes = MongoFilterBuilder.GetFiltersDefinition(filters);
        if (filterRes.IsFailed)
        {
            return Result.Fail(filterRes.Errors);
        }
        var res =await (await collection.FindAsync(filterRes.Value)).ToListAsync();
        return res.ToRecords().ToArray();
    }
}