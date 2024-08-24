using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public sealed class MongoNosqlDao:INosqlDao 
{
    private readonly IMongoDatabase _mongoDatabase ;

    public MongoNosqlDao(string connectionString, string database)
    {
        var client = new MongoClient(connectionString);
        _mongoDatabase = client.GetDatabase(database);

    }

    public async Task Insert(string collectionName, IEnumerable<Record> items)
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