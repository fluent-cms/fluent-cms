using FluentCMS.Test.Util;
using FluentCMS.Utils.ApiClient;

namespace FluentCMS.Blog.DistributeTest;

public class DistributeTest
{
    private const string Post = "distribut_test_post";
    private const string Title = "title";
    
    private const string Addr1 = "http://localhost:5133";
    private const string Addr2 = "http://localhost:5134";

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
        
        var res = (await _followerQuery.SendSingleGraphQuery(entityName, ["id",Title])).AssertSuccess();

        (await _leaderSchema.DeleteSchema(schema.Id)).AssertSuccess();
        
        (await _followerQuery.SendSingleGraphQuery(entityName, [Title])).AssertFail();
    }
}