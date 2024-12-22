using System.Text.Json;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.App.Tests;

//have to start FluentCMS.App.AppHost manually
public class QueryTest
{
  private readonly QueryApiClient _queryApi = new(new HttpClient { BaseAddress = new Uri("http://localhost:5001") });

  [Fact]
  public async Task FilterInSubField()
  {
    var items = (await _queryApi.ListGraphQl("post", ["id", "title"], "post_test")).Ok();
  }

  [Fact]
  public async Task FilterExpress()
  {
    var raw = """
              query post_test{
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
      var raw = """
                query post_test{
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
  
  
}