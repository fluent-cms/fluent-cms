using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.IntegrationTests;

public class QueryApiTest
{
    private const string TableName = "view_test";
    private const string FieldName = "name";
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
    public async void List()
    {
        await PrepareEntity();
        var query = GetQuery(TableName);
        (await _schemaApiClient.SaveSchema(query)).AssertSuccess();
        var res = (await _queryApiClient.GetList(query.Name, new Cursor(), new Pagination())).AssertSuccess();
    }

    [Fact]
    public async void Many()
    {
        await PrepareEntity();
        var query = GetQuery(TableName);
        (await _schemaApiClient.SaveSchema(query)).AssertSuccess();
        var ids = Enumerable.Range(1, 5).ToArray().Select(x=>x as object).ToArray();
        Assert.Equal(QueryPageSize,(await _queryApiClient.GetMany(query.Name,ids )).AssertSuccess().Length);
    }
    [Fact]
    public async void One()
    {
        await PrepareEntity();
        var query = GetQuery(TableName);
        (await _schemaApiClient.SaveSchema(query)).AssertSuccess();
        Assert.NotNull((await _queryApiClient.GetOne(query.Name,1)).AssertSuccess());
    }
    async Task PrepareEntity()
    {
         await _accountApiClient.EnsureLogin();
         await _schemaApiClient.EnsureSimpleEntity(TableName, FieldName);
         for (var i = 0; i < QueryPageSize * 2 + 1; i++)
         {
             (await _entityApiClient.AddSimpleData(TableName, FieldName, $"{TableName}-{i}")).AssertSuccess();
         }
    }

    Schema GetQuery(string tableName)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var filter = new Filter("id", "and", [new Constraint(Matches.In, "qs.id")], true);
        var query = new Query(tableName + suffix, tableName, QueryPageSize, "{id," + FieldName + "}",
            [new Sort("id", SortOrder.Asc)], [filter]);
        return new Schema
        (
            Name : query.Name,
            Type : SchemaType.Query,
            Settings : new Settings
            {
                Query = query
            }
        );
    }
 
}