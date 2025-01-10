using FormCMS.Core.Descriptors;
using FluentResults;

namespace FormCMS.CoreKit.DocDbQuery;

public interface IDocumentDbDao
{
    Task Upsert(string collection, string primaryKey, Record record);
    Task Upsert(string collection, string primaryKey, object primaryKeyValue,object document);
    Task Delete(string collection, string id);
    Task BatchInsert(string collection, IEnumerable<Record> records);
    Task<Record[]> All(string collection);
}