using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.ResultExt;
using IdGen;

namespace FluentCMS.Blog.Tests;

public class EntityApiTest
{
    private const string Name = "name";
    private static string PostEntityName() => "post" + new IdGenerator(0).CreateId();
    private static string AuthorEntityName() => "author" + new IdGenerator(0).CreateId();
    private static string TagEntityName() => "tag" + new IdGenerator(0).CreateId();

    private readonly AccountApiClient _accountApiClient;
    private readonly EntityApiClient _entityApiClient;
    private readonly SchemaApiClient _schemaApiClient;

    public EntityApiTest()
    {
        Util.SetTestConnectionString();

        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
    }



    [Fact]
    public async Task InsertListDeleteOk()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        var entityName = PostEntityName();
        (await _schemaApiClient.EnsureSimpleEntity(entityName, Name)).Ok();
        var item = (await _entityApiClient.Insert(entityName, Name, "post1")).Ok();

        var res = (await _entityApiClient.List(entityName, 0, 10)).Ok();
        Assert.Single(res.Items);

        (await _entityApiClient.Delete(entityName, item)).Ok();
        res = (await _entityApiClient.List(entityName, 0, 10)).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task InsertAndUpdateAndOneOk()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        var entityName = PostEntityName();
        (await _schemaApiClient.EnsureSimpleEntity(entityName, Name)).Ok();
        var item = (await _entityApiClient.Insert(entityName, Name, "post1")).Ok();
        Assert.True(item.TryGetValue("id", out var element));
        Assert.Equal(1, element.GetInt32());

        (await _entityApiClient.Update(entityName, 1, Name, "post2")).Ok();

        item = (await _entityApiClient.One(entityName, 1)).Ok();
        Assert.True(item.TryGetValue(Name, out element));
        Assert.Equal("post2", element.GetString());
    }

    [Fact]
    public async Task ListWithPaginationOk()
    {
        var post = PostEntityName();

        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(post, Name)).Ok();
        for (var i = 0; i < 5; i++)
        {
            (await _entityApiClient.Insert(post, Name, $"student{i}")).Ok();
        }

        (await _entityApiClient.Insert(post, Name, "good-student")).Ok();
        (await _entityApiClient.Insert(post, Name, "good-student")).Ok();

        //get first page
        Assert.Equal(5, (await _entityApiClient.List(post, 0, 5)).Ok().Items.Length);
        //get last page
        var res = (await _entityApiClient.List(post, 5, 5)).Ok();
        Assert.Equal(2, res.Items.Length);
    }

    [Fact]
    public async Task InsertWithLookupWithWrongData()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        var (postEntityName, authorEntityName) = (PostEntityName(), AuthorEntityName());
        (await _schemaApiClient.EnsureSimpleEntity(authorEntityName, Name)).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(postEntityName, Name, authorEntityName, "")).Ok();
        var res = await _entityApiClient.InsertWithLookup(postEntityName, Name, "post1",
            authorEntityName, "author1");
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task InsertWithLookup()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        var (postEntityName, authorEntityName) = (PostEntityName(), AuthorEntityName());
        (await _schemaApiClient.EnsureSimpleEntity(authorEntityName, Name)).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(postEntityName, Name, authorEntityName, "")).Ok();
        var author = (await _entityApiClient.Insert(authorEntityName, Name, "author1")).Ok();
        (await _entityApiClient.InsertWithLookup(postEntityName, Name, "post1",
            authorEntityName, author["id"].GetInt32())).Ok();
    }

    [Fact]
    public async Task InsertWithJunction()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        
    }
}