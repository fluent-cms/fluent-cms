using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public sealed class MongoDao:IDao 
{
    private readonly IMongoDatabase _mongoDatabase ;

    public MongoDao(string connectionString, string database)
    {
        var client = new MongoClient(connectionString);
        _mongoDatabase = client.GetDatabase(database);

    }

    public async Task Insert(string collectionName, Record[] items)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var docs = items.Select(x => new BsonDocument(x));
        await collection.InsertManyAsync(docs);
    }

    public async Task List(string collectionName)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
        var ret = await collection.Find(new BsonDocument()).Limit(10).ToListAsync();

    }
}