using System.Text.Json;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.JsonUtil;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using IdGen;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;

public class EntityApiTest
{
    private const string Name = "name";
    private readonly  string _postEntityName = "entity_api_test_post" + new IdGenerator(0).CreateId();
    private readonly string _authorEntityName = "entity_api_test_author" + new IdGenerator(0).CreateId();
    private readonly string _tagEntityName = "entity_api_test_tag" + new IdGenerator(0).CreateId();
    private readonly string _attachmentEntityName = "entity_api_test_attachment" + new IdGenerator(0).CreateId();
    private readonly string _category = "entity_api_test_category" + new IdGenerator(0).CreateId();

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
        _accountApiClient.EnsureLogin().Wait();
    }

    
    
    [Fact]
    public async Task TestResponseMode()
    {
        await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name).Ok();
        await _entityApiClient.Insert(_postEntityName, Name, "post1").Ok();
        var response = await _entityApiClient.List(_postEntityName, 0, 1, "count").Ok();
        Assert.True(response.Items.Length == 0);

        response = await _entityApiClient.List(_postEntityName, 0, 1, "items").Ok();
        Assert.True(response.TotalRecords == 0);
    }
    
    [Fact]
    public async Task GetResultAsTree()
    {
        await _schemaApiClient.EnsureEntity(new Entity(
            Attributes:[
                new Attribute("name", "Name"),
                new Attribute("parent", "Parent",DataType: DataType.Int, DisplayType: DisplayType.Number),
                new Attribute("children", "Children",
                    DataType: DataType.Collection, 
                    DisplayType: DisplayType.EditTable,
                    Options: $"{_category}.parent"),
            ],
            Name:_category,
            TableName:_category,
            Title:"name",
            TitleAttribute:"name",
            DefaultPageSize:EntityConstants.DefaultPageSize
        )).Ok();
        await _entityApiClient.Insert(_category, new { name = "cat1", }).Ok();
        await _entityApiClient.Insert(_category, new { name = "cat2", }).Ok();
        await _entityApiClient.Insert(_category, new { name = "cat3", }).Ok();
        await _entityApiClient.CollectionInsert(_category, "children", 1, new { name= "cat1-1" }).Ok();
        await _entityApiClient.CollectionInsert(_category, "children", 1, new { name= "cat1-2" }).Ok();
        var items = await _entityApiClient.ListAsTree(_category).Ok();
        var children = items[0].GetProperty("children");
        Assert.True(children.ValueKind == JsonValueKind.Array);
        Assert.Equal(2, children.GetArrayLength());
    }


    [Fact]
    public async Task InsertListDeleteOk()
    {
        await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name).Ok();
        var item = await _entityApiClient.Insert(_postEntityName, Name, "post1").Ok();

        var res = await _entityApiClient.List(_postEntityName, 0, 10).Ok();
        Assert.Single(res.Items);

        await _entityApiClient.Delete(_postEntityName, item).Ok();
        res = await _entityApiClient.List(_postEntityName, 0, 10).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task InsertAndUpdateAndOneOk()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name)).Ok();
        var item = (await _entityApiClient.Insert(_postEntityName, Name, "post1")).Ok();
        Assert.True(item.ToDictionary().TryGetValue("id", out var element));
        Assert.Equal(1, element);

        (await _entityApiClient.Update(_postEntityName, 1, Name, "post2")).Ok();

        item = (await _entityApiClient.One(_postEntityName, 1)).Ok();
        Assert.True(item.ToDictionary().TryGetValue(Name, out element));
        Assert.Equal("post2", element);
    }

    [Fact]
    public async Task ListWithPaginationOk()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name)).Ok();
        for (var i = 0; i < 5; i++)
        {
            (await _entityApiClient.Insert(_postEntityName, Name, $"student{i}")).Ok();
        }

        (await _entityApiClient.Insert(_postEntityName, Name, "good-student")).Ok();
        (await _entityApiClient.Insert(_postEntityName, Name, "good-student")).Ok();

        //get first page
        Assert.Equal(5, (await _entityApiClient.List(_postEntityName, 0, 5)).Ok().Items.Length);
        //get last page
        var res = (await _entityApiClient.List(_postEntityName, 5, 5)).Ok();
        Assert.Equal(2, res.Items.Length);
    }

    [Fact]
    public async Task InsertWithLookupWithWrongData()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_authorEntityName, Name)).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name,lookup: _authorEntityName)).Ok();
        var res = await _entityApiClient.InsertWithLookup(_postEntityName, Name, "post1",
            _authorEntityName, "author1");
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task InsertWithLookup()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_authorEntityName, Name)).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name, lookup:_authorEntityName)).Ok();
        var author = (await _entityApiClient.Insert(_authorEntityName, Name, "author1")).Ok();
        (await _entityApiClient.InsertWithLookup(_postEntityName, Name, "post1",
            _authorEntityName, author.ToDictionary()["id"])).Ok();
    }

    [Fact]
    public async Task InsertDeleteListJunction()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_tagEntityName, Name)).Ok();
        (await _entityApiClient.Insert(_tagEntityName, Name, "tag1")).Ok();

        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name, junction: _tagEntityName)).Ok();
        (await _entityApiClient.Insert(_postEntityName, Name, "post1")).Ok();
        

        (await _entityApiClient.JunctionAdd(_postEntityName, _tagEntityName, 1, 1)).Ok();
        var res = (await _entityApiClient.JunctionList(_postEntityName, _tagEntityName, 1, true)).Ok();
        Assert.Empty(res.Items);
        
        var ids = (await _entityApiClient.JunctionTargetIds(_postEntityName, _tagEntityName, 1)).Ok();
        Assert.Single(ids);
        
        res = (await _entityApiClient.JunctionList(_postEntityName, _tagEntityName, 1, false)).Ok();
        Assert.Single(res.Items);

        (await _entityApiClient.JunctionDelete(_postEntityName, _tagEntityName, 1, 1)).Ok();
        res = (await _entityApiClient.JunctionList(_postEntityName, _tagEntityName, 1, true)).Ok();
        Assert.Single(res.Items);
        res = (await _entityApiClient.JunctionList(_postEntityName, _tagEntityName, 1, false)).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task LookupApiWorks()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_tagEntityName, Name)).Ok();
        for (var i = 0; i < EntityConstants.DefaultPageSize - 1; i++)
        {
            await _entityApiClient.Insert(_tagEntityName,Name,$"tag{i}");
        }

        var res = (await _entityApiClient.LookupList(_tagEntityName,"")).Ok();
        Assert.False(res.GetProperty("hasMore").GetBoolean());

        for (var i = EntityConstants.DefaultPageSize; i < EntityConstants.DefaultPageSize + 10; i++)
        {
            await _entityApiClient.Insert(_tagEntityName,Name,$"tag{i}");
        }
        
        res = (await _entityApiClient.LookupList(_tagEntityName,"")).Ok();
        Assert.True(res.GetProperty("hasMore").GetBoolean());

        res = (await _entityApiClient.LookupList(_tagEntityName,"tag11")).Ok();
        Assert.True(res.GetProperty("hasMore").GetBoolean());
        Assert.Equal(1,res.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task CollectionApiWorks()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name)).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_attachmentEntityName, Name, _postEntityName)).Ok();
        (await _schemaApiClient.EnsureSimpleEntity(_postEntityName, Name,collection:_attachmentEntityName,linkAttribute:_postEntityName)).Ok();

        await _entityApiClient.Insert(_postEntityName, Name, "post1").Ok();

        await _entityApiClient.CollectionInsert(_postEntityName, _attachmentEntityName, 1,
            new Dictionary<string, object> {{Name,"attachment1" }}).Ok();

        var listResponse = await _entityApiClient.CollectionList(_postEntityName, _attachmentEntityName, 1).Ok();
        Assert.Single(listResponse.Items);

        await _entityApiClient.Delete(_attachmentEntityName, new { id = 1 });
        listResponse = await _entityApiClient.CollectionList(_postEntityName, _attachmentEntityName, 1).Ok();
        Assert.Empty(listResponse.Items);

    }

}