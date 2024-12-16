using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Blog.Tests;

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
        Util.SetTestConnectionString();

        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
        _queryApiClient = new QueryApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task TestRelatedData()
    {
        var postId =await PrepareOneRelatedData();
        var query = GetPostQuery();
        (await _schemaApiClient.SaveSchema(query)).Ok();
        var res = (await _queryApiClient.GetOne(query.Name, postId)).Ok();
        var author = res[Author] as Dictionary<string, object>;
        Assert.True(author?.ContainsKey(Author) == true);
        var tags = res[Tag] as object[];
        Assert.True(tags?.Length > 0);
    }


    [Fact]
    public async Task List()
    {
        await PrepareSimpleData();
        var query = GetPostQuery();
        (await _schemaApiClient.SaveSchema(query)).Ok();
        var items = (await _queryApiClient.GetList(query.Name, new Span(), new Pagination())).Ok();
        if (!SpanHelper.HasNext(items)) return;
        var res = await _queryApiClient.GetList(query.Name, new Span(Last: SpanHelper.LastCursor(items)), new Pagination());
        res.Ok();
    }

    [Fact]
    public async Task Many()
    {
        await PrepareSimpleData();
        var query = GetPostQuery();
        (await _schemaApiClient.SaveSchema(query)).Ok();
        var ids = Enumerable.Range(1, 5).ToArray().Select(x => x as object).ToArray();
        var res = (await _queryApiClient.GetMany(query.Name,"id", ids)).Ok();
        Assert.Equal(ids.Length, res.Length);
    }

    async Task PrepareSimpleData()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        await _schemaApiClient.EnsureSimpleEntity(Author, Author );
        await _schemaApiClient.EnsureSimpleEntity(Tag, Tag );
        await _schemaApiClient.EnsureSimpleEntity(Post, Post, Author, Tag);

        for (var i = 0; i < QueryPageSize * 2 + 1; i++)
        {
            (await _entityApiClient.Insert(Post, Post, $"{Post} {i}" )).Ok();
        }
    }

    async Task<int> PrepareOneRelatedData()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        await _schemaApiClient.EnsureSimpleEntity(Author, Author );
        await _schemaApiClient.EnsureSimpleEntity(Tag, Tag );
        await _schemaApiClient.EnsureSimpleEntity(Post, Post, Author, Tag);
        var authorId = (await _entityApiClient.Insert(Author, Author, Author)).Ok()["id"].GetInt32();
        var postId = (await _entityApiClient.InsertWithLookup(Post, Post, $"post ", Author, authorId))
            .Ok()["id"].GetInt32();

        for (var i = 0; i < QueryPageSize * 2 + 1; i++)
        {
            var tag = (await _entityApiClient.Insert(Tag, Tag, $"{Tag} {i}")).Ok();
            (await _entityApiClient.AddJunctionData(Post, Tag, postId, tag["id"].GetInt32())).Ok();
        }

        return postId;
    }

    static Schema GetPostQuery()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var query = new Query(
            Name: Post + suffix,
            EntityName: Post,
            Source: $$"""
                          { 
                            id, {{Post}},
                            {{Author}}{id, {{Author}} },
                            {{Tag}}{id, {{Tag}}}
                          }
                          """,
            Sorts: [new Sort("id", SortOrder.Desc)],
            Filters: [
                new Filter("id", "and", [new Constraint(Matches.In,["$id"] )])
            ],
            ReqVariables:[]
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
