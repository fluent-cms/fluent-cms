using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using IdGen;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Blog.Tests;

public class QueryApiTest
{
    private const string Name = "name";
    private readonly string _post = "post" + new IdGenerator(0).CreateId();
    private readonly string _tag = "tag" + new IdGenerator(0).CreateId();

    private readonly EntityApiClient _entity;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly AccountApiClient _account;
    private readonly QueryApiClient _query;


    public QueryApiTest()
    {
        Util.SetTestConnectionString();

        WebAppClient<Program> webAppClient = new();
        _entity = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _account = new AccountApiClient(webAppClient.GetHttpClient());
        _query = new QueryApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task List()
    {
        var limit = 4;
        (await _account.EnsureLogin()).Ok();
        await AddTags(limit + 1);
        (await _query.ListGraphQl(_tag, ["id", Name])).Ok();
        
        var items = (await _query.List(query:_tag, limit:limit)).Ok();
        Assert.Equal(limit, items.Length);
        
        if (!SpanHelper.HasNext(items.Last().ToDictionary())) return;
        items  = (await _query.List(query:_tag,last: SpanHelper.Cursor(items.Last()),limit:limit)).Ok();
        Assert.Single(items);
    }
    
    [Fact]
    public async Task Many()
    {
        (await _account.EnsureLogin()).Ok();
        await AddTags(2);
        (await _query.ListGraphQl(_tag, ["id", Name])).Ok();
        var item = (await _query.Many(_tag, [1,2])).Ok();
        Assert.Equal(2, item.Length);
    }

    [Fact]
    public async Task Single()
    {
        (await _account.EnsureLogin()).Ok();
        await AddTags(1);
        (await _query.ListGraphQl(_tag, ["id", Name])).Ok();
        var item = (await _query.Single(_tag, 1)).Ok();
        Assert.Equal(1, item.ToDictionary()["id"]);
    }

    [Fact]
    public async Task Part()
    {
        (await _account.EnsureLogin()).Ok();
        await AddTags(6);
        await AddPosts(1);
        await AddPostTagJunction(1, 6);
        (await _query.ListGraphQlJunction(_post, ["id", Name],_tag,["id",Name])).Ok();
        
        var posts = (await _query.ListArgs(_post, new Dictionary<string, StringValues>
        {
            [$"{_tag}.limit"] = "4",
        })).Ok();

        var post = posts[0].ToDictionary();
        if (post.TryGetValue(_tag, out var v) 
            && v is object[] arr 
            && arr.Last() is Dictionary<string,object> lastTag)
        {
            
            var cursor = SpanHelper.Cursor(lastTag);
            var tags = (await _query.Part(query: _post,attr:_tag, last: cursor,limit:10)).Ok();
            Assert.Equal(2,tags.Length);
        }
        else
        {
            Assert.Fail("didn't find tag");
        }
    }

    [Fact]
    public async Task SingleAndListGraphQlOk()
    {
        (await _account.EnsureLogin()).Ok();
        await AddTags(6);
        var res = (await _query.ListGraphQl(_tag, ["id", Name])).Ok();
        Assert.Equal(6, res.Length);
        var ele = (await _query.SingleGraphQl(_tag, ["id", Name])).Ok();
        Assert.Equal(2,ele.ToDictionary().Count);
    }

    [Fact]
    public async Task ListGraphQlByIdsOk()
    {
        (await _account.EnsureLogin()).Ok();
        await AddTags(6);
        var res = (await _query.ListGraphQlByIds(_tag, [1,2])).Ok();
        Assert.Equal(2, res.Length);
    }
    
    private async Task AddTags(int count)
    {
        (await _schemaApiClient.EnsureSimpleEntity(_tag, Name)).Ok();
        for (var i = 0; i < count; i++)
        {
            (await _entity.Insert(_tag,Name,$"tag{i}")).Ok();
        }
    }

    private async Task AddPosts(int count)
    {
        (await _schemaApiClient.EnsureSimpleEntity(_post, Name,"",_tag)).Ok();
        for (var i = 0; i < count; i++)
        {
            (await _entity.Insert(_post,Name,$"post{i}")).Ok();
        }
    }

    private async Task AddPostTagJunction(int postCount, int tagCount)
    {
        for (var i = 0; i < postCount; i++)
        {
            for (var j = 0; j < tagCount; j++)
            {
                (await _entity.JunctionAdd(_post, _tag, i + 1, j + 1)).Ok();
            }
        }
    }
} 