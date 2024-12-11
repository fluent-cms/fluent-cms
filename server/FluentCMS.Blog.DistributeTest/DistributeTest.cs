using FluentCMS.Test.Util;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Blog.DistributeTest;

public class DistributeTest
{
    private const string Post = "distribut_test_post";
    private const string Title = "title";
    
    private const string Addr1 = "http://localhost:5134";
    private const string Addr2 = "http://localhost:5135";

    private readonly SchemaApiClient _leaderSchema;
    private readonly EntityApiClient _leaderEntity;
    private readonly QueryApiClient _leaderQuery;
    private readonly AccountApiClient _leaderAccount;
    
    private readonly QueryApiClient _followerQuery;

    public DistributeTest()
    {
        var httpClient1 = new HttpClient
        {
            BaseAddress = new Uri(Addr1)
        };
        var httpClient2 = new HttpClient
        {
            BaseAddress = new Uri(Addr2)
        };
         
        _leaderAccount = new AccountApiClient(httpClient1);
        _leaderSchema = new SchemaApiClient(httpClient1);
        _leaderEntity = new EntityApiClient(httpClient1);
        _leaderQuery = new QueryApiClient(httpClient1);
        
        _followerQuery = new QueryApiClient(httpClient2);
    }

    string EntityName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return Post + suffix;
        
    }
    [Fact]
    public async Task EntityChange()
    {
        await _leaderAccount.EnsureLogin();
        
        var entityName = EntityName();
        var schema = (await _leaderSchema.EnsureSimpleEntity(entityName, Title)).AssertSuccess();
        await _leaderEntity.AddSimpleData(entityName, Title,"title1");
        
        Thread.Sleep(TimeSpan.FromSeconds(6));
        (await _followerQuery.SendSingleGraphQuery(entityName, ["id",Title])).AssertSuccess();
        (await _leaderSchema.DeleteSchema(schema.Id)).AssertSuccess();
        Thread.Sleep(TimeSpan.FromSeconds(6)); 
        (await _followerQuery.SendSingleGraphQuery(entityName, [Title])).AssertFail();
    }

    [Fact]
    public async Task QueryChange()
    {
        await _leaderAccount.EnsureLogin();

        var entityName = EntityName();
        (await _leaderSchema.EnsureSimpleEntity(entityName, Title)).AssertSuccess();
        await _leaderEntity.AddSimpleData(entityName, Title, "title1");
        
        (await _leaderQuery.SendSingleGraphQuery(entityName, ["id"], true)).AssertSuccess();
        Thread.Sleep(TimeSpan.FromSeconds(6));
        var result = (await _followerQuery.GetList(entityName, new Span(), new Pagination())).AssertSuccess();
        Assert.Equal(4,result.First().Count);
        
        (await _leaderQuery.SendSingleGraphQuery(entityName, ["id", Title],true)).AssertSuccess();
        Thread.Sleep(TimeSpan.FromSeconds(6));
        result =(await _followerQuery.GetList(entityName, new Span(), new Pagination())).AssertSuccess();
        Assert.Equal(5, result.First().Count);
    }
}