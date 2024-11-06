using System.Text.Json;
using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using Xunit.Abstractions;

namespace FluentCMS.IntegrationTests;

public class QueryApiTest
{
    private const string Post = "query_api_test_post";
    private const string Author = "query_api_test_author";
    private const string Tag = "query_api_test_tag";
    private const int QueryPageSize = 4;

    private readonly EntityApiClient _entityApiClient;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly AccountApiClient _accountApiClient;
    private readonly QueryApiClient _queryApiClient;


    public QueryApiTest()
    {
        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
        _queryApiClient = new QueryApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async void TestRelatedData()
    {
        var postId =await PrepareOneRelatedData();
        var query = GetPostQuery();
        (await _schemaApiClient.SaveSchema(query)).AssertSuccess();
        var res = (await _queryApiClient.GetOne(query.Name, postId)).AssertSuccess();
        var author = res[Author] as Dictionary<string, object>;
        Assert.True(author?.ContainsKey(Author) == true);
        var tags = res[Tag] as object[];
        Assert.True(tags?.Length > 0);
    }


    [Fact]
    public async void List()
    {
        await PrepareSimpleData();
        var query = GetPostQuery();
        (await _schemaApiClient.SaveSchema(query)).AssertSuccess();
        var items = (await _queryApiClient.GetList(query.Name, new Span(), new Pagination())).AssertSuccess();
        if (!SpanHelper.HasNext(items)) return;
        var res = await _queryApiClient.GetList(query.Name, new Span(Last: SpanHelper.LastCursor(items)), new Pagination());
        res.AssertSuccess();
    }

    [Fact]
    public async void Many()
    {
        await PrepareSimpleData();
        var query = GetPostQuery();
        (await _schemaApiClient.SaveSchema(query)).AssertSuccess();
        var ids = Enumerable.Range(1, 5).ToArray().Select(x => x as object).ToArray();
        Assert.Equal(QueryPageSize, (await _queryApiClient.GetMany(query.Name, ids)).AssertSuccess().Length);

    }

    async Task PrepareSimpleData()
    {
        await _accountApiClient.EnsureLogin();
        await _schemaApiClient.EnsureSimpleEntity(Author, Author );
        await _schemaApiClient.EnsureSimpleEntity(Tag, Tag );
        await _schemaApiClient.EnsureSimpleEntity(Post, Post, Author, Tag);

        for (var i = 0; i < QueryPageSize * 2 + 1; i++)
        {
            (await _entityApiClient.AddSimpleData(Post, Post, $"{Post} {i}" )).AssertSuccess();
        }
    }

    async Task<int> PrepareOneRelatedData()
    {
        await _accountApiClient.EnsureLogin();
        await _schemaApiClient.EnsureSimpleEntity(Author, Author );
        await _schemaApiClient.EnsureSimpleEntity(Tag, Tag );
        await _schemaApiClient.EnsureSimpleEntity(Post, Post, Author, Tag);
        var authorId = (await _entityApiClient.AddSimpleData(Author, Author, Author)).AssertSuccess()["id"].GetInt32();
        var postId = (await _entityApiClient.AddDataWithLookup(Post, Post, $"post ", Author, authorId))
            .AssertSuccess()["id"].GetInt32();

        for (var i = 0; i < QueryPageSize * 2 + 1; i++)
        {
            var tag = (await _entityApiClient.AddSimpleData(Tag, Tag, $"{Tag} {i}")).AssertSuccess();
            (await _entityApiClient.AddCrosstableData(Post, Tag, postId, tag["id"].GetInt32())).AssertSuccess();
        }

        return postId;
    }

    static Schema GetPostQuery()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var query = new Query(
            Name: Post + suffix,
            EntityName: Post,
            PageSize: QueryPageSize,
            SelectionSet: $$"""
                          { 
                            id, {{Post}},
                            {{Author}}{id, {{Author}} },
                            {{Tag}}{id, {{Tag}}}
                          }
                          """,
            Sorts: [new Sort("id", SortOrder.Desc)],
            Filters: [
                new Filter("id", "and", [new Constraint(Matches.In, "qs.id")], true)
            ]
        );
        return new Schema
        (
            Name: query.Name,
            Type: SchemaType.Query,
            Settings: new Settings
            {
                Query = query
            }
        );
    }
} 
