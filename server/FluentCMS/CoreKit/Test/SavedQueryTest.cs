using System.Text.Json;
using FluentCMS.CoreKit.ApiClient;
using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.CoreKit.Test;

public class SavedQueryTest(QueryApiClient client, string queryName)
{
    public async Task PaginationByCursor()
    {
        await $$"""
                query {{queryName}}{
                  postList{ id }
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();

        var items = (await client.List(queryName)).Ok();
        var firstId = items.First().Id();

        items = (await client.List(queryName, last: SpanHelper.Cursor(items.Last()))).Ok();
        SimpleAssert.IsTrue(items.First().Id() > firstId);

        items = (await client.List(queryName, first: SpanHelper.Cursor(items.First()))).Ok();
        SimpleAssert.AreEqual(items.First().Id(), firstId);
    }

    public async Task VerifyRecordCount()
    {
        const int limit = 10;
        (await client.ListGraphQl("post", ["id"], queryName)).Ok();
        var items = (await client.List(query: queryName, limit: limit)).Ok();
        SimpleAssert.AreEqual(limit, items.Length);
    }

    public async Task VerifyManyApi()
    {
        await $$"""
                query {{queryName}}($id:Int){
                    postList(idSet:[$id]){id}
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();
        var items = (await client.Many(queryName, [1, 2])).Ok();
        SimpleAssert.AreEqual(2, items.Length);
    }
    
    
    public async Task VerifySingleApi()
    {
        await $$"""
                query {{queryName}}($id:Int){
                    postList(idSet:[$id]){id}
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();
        var items = (await client.Single(queryName, 1)).Ok();
        SimpleAssert.IsTrue(items.HasId());
    }
}