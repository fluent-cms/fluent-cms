using System.Text.Json;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.App.Tests;

//have to start FluentCMS.App.AppHost manually
public class QueryTest
{
  private readonly QueryApiClient _queryApi = new(new HttpClient { BaseAddress = new Uri("http://localhost:5001") });
  private const string TestQueryName = "post_test";

  [Fact]
  public async Task FilterInSubField()
  {
    var items = (await _queryApi.ListGraphQl("post", ["id", "title"], "post_test")).Ok();
  }

  [Fact]
  public async Task FilterExpress()
  {
    const string raw = $$"""
                         query {{TestQueryName}}{
                           postList(
                             filterExpr:[
                               {
                                 field:"author.name",
                                 clause:[{
                                   startsWith:"authors.name-0-2"
                                 }]
                               }
                             ]
                           ){
                             id
                           }
                         }
                         """;
    (await _queryApi.SendGraphQuery<JsonElement[]>(raw)).Ok();
  }

  [Fact]
  public async Task SortExpr()
  {
    const string raw = $$"""
                         query {{TestQueryName}}{
                           postList(
                             id:{lt:1000},
                             sortExpr:[
                               {
                                 field:"category.name",order:Desc
                               }
                             ]
                           ){
                             id, title, body,abstract
                             tag{id,name}
                             category{id,name}
                             author{id,name}
                           }
                         }
                         """;
    (await _queryApi.SendGraphQuery<JsonElement[]>(raw)).Ok();
  }

  [Fact]
  public async Task PageinationWork()
  {
    const string raw = $$"""
                         query {{TestQueryName}}{
                           postList{
                             id 
                           }
                         }
                         """;
    (await _queryApi.SendGraphQuery<JsonElement[]>(raw)).Ok();
    var items = (await _queryApi.List(TestQueryName)).Ok();
    var firstPageLastId = items.Last().GetProperty("id").GetInt32();
    var firstPageLastCursor = SpanHelper.Cursor(items.Last());
    
    items = (await _queryApi.List(TestQueryName,last:firstPageLastCursor)).Ok();
    Assert.True( items.First().GetProperty("id").GetInt32()> firstPageLastId);

    items = (await _queryApi.List(TestQueryName, first: SpanHelper.Cursor(items.First()))).Ok();
    Assert.True( items.Last().GetProperty("id").GetInt32() == firstPageLastId);
  }
}