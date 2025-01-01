using System.Text.Json;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.Utils.Test;

/*
    These query test cases are designed to run against both
    DocumentDB (configured in FluentCMS.App.Test) and RelationDB (configured in FluentCMS.Blog.Test).

    Note: Test cases for additional features supported by RelationDB
    (e.g., query arguments for sort, filter, and pagination on subfields)
    are excluded here to ensure compatibility with DocumentDB.

    To test against DocumentDB:
    1. Verify that the entity 'post' is mapped to the DocumentDB collection 'post' via the apiLinksArray.
    2. Verify that 'queryName' is mapped to the DocumentDB collection 'post' via the queryLinksArray.
*/

public class BlogsTestCases(QueryApiClient client, string queryName)
{
    public async Task VerifySingleApi()
    {
        await $$"""
                query {{queryName}}($id:Int){
                    postList(idSet:[$id]){id}
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();
        var items = (await client.Single(queryName, 1)).Ok();
        if (items.GetProperty("id").GetInt32() != 1) 
            throw new Exception("Failed to verify Single API, wrong return value");
    }

    public async Task VerifyManyApi()
    {
        await $$"""
                query {{queryName}}($id:Int){
                    postList(idSet:[$id]){id}
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();
        var items = (await client.Many(queryName, [1, 2])).Ok();
        if (items.Length != 2) 
            throw new Exception("Failed to verify Many API,invalid number of items");
    }

    public async Task VerifySort()
    {
        var items = await $$"""
                            query {{queryName}}{
                                postList(sort:idDesc){id}
                            }
                            """.GraphQlQuery<JsonElement[]>(client).Ok();
        CheckOrder(false);

        items = await $$"""
                        query {{queryName}}{
                            postList(sort:idDesc){id}
                        }
                        """.GraphQlQuery<JsonElement[]>(client).Ok();
        CheckOrder(true);


        void CheckOrder(bool isAscending)
        {
            var id1 = items[0].GetProperty("id").GetInt32();
            var id2 = items[0].GetProperty("id").GetInt32();
            if (isAscending && id1 < id2 || !isAscending && id1 > id2) throw new Exception("wrong order");
        }
    }

    public async Task VerifyRecordCount()
    {
        const int limit = 10;
        (await client.ListGraphQl("post", ["id"], queryName)).Ok();
        var items = (await client.List(query: queryName, limit: limit)).Ok();
        if (limit != items.Length) throw new Exception($"Limit {limit} does not match");
    }

    public async Task VerifyFilterExpression()
    {
        var items =await $$"""
                 query {{queryName}}{
                   postList(
                     filterExpr:[
                       {
                         field:"authors.name",
                         clause:[{
                           startsWith:"name"
                         }]
                       }
                     ]
                   ){
                     id
                   }
                 }
                 """.GraphQlQuery<JsonElement[]>(client).Ok();
        if(items.Length == 0) throw new Exception($"Verify Filter Expression Fail, No records found");
    }

    public async Task VerifySortExpression()
    {
        var items = await $$"""
                 query {{queryName}}{
                   postList(
                     sortExpr:[
                       {
                         field:"category.name",order:Desc
                       }
                     ]
                   ){
                     id
                   }
                 }
                 """.GraphQlQuery<JsonElement[]>(client).Ok();
        CheckOrder(false);
        void CheckOrder(bool isAscending)
        {
            var id1 = items[0].GetProperty("id").GetInt32();
            var id2 = items[0].GetProperty("id").GetInt32();
            if (isAscending && id1 < id2 || !isAscending && id1 > id2) throw new Exception("wrong order");
        }
    }


    public async Task VerifyPagination()
    {
        await $$"""
                query {{queryName}}{
                  postList{ id }
                }
                """.GraphQlQuery<JsonElement[]>(client).Ok();

        var items = (await client.List(queryName)).Ok();
        var firstPageLastId = items.Last().GetProperty("id").GetInt32();
        var firstPageLastCursor = SpanHelper.Cursor(items.Last());

        items = (await client.List(queryName, last: firstPageLastCursor)).Ok();
        if (!(items.First().GetProperty("id").GetInt32() > firstPageLastId))
            throw new Exception("first id should bigger than last page's id");

        items = (await client.List(queryName, first: SpanHelper.Cursor(items.First()))).Ok();
        if (items.Last().GetProperty("id").GetInt32() != firstPageLastId)
            throw new Exception("back to first page should get same id");
    }
}
