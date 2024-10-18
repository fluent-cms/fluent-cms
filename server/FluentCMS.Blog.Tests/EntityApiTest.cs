using FluentCMS.Utils.ApiClient;

namespace FluentCMS.IntegrationTests;
public class EntityApiTest 
{
    private const string Leaner = "leaner1";
    private const string Name = "name1";

    private readonly AccountApiClient _accountApiClient;
    private readonly EntityApiClient _entityApiClient;
    private readonly SchemaApiClient _schemaApiClient;

    public EntityApiTest()
    {
        /*
        Environment.SetEnvironmentVariable("DatabaseProvider", "Sqlite");
        string dbName = Path.Combine(Directory.GetCurrentDirectory(),"_cms.db"); 
        Environment.SetEnvironmentVariable("Sqlite", $"Data Source={dbName}");
        */

        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
    }


    [Fact]
    public async Task EntityRetrieve()
    {
        await _accountApiClient.EnsureLogin();
        (await _schemaApiClient.AddSimpleEntity(Leaner, Name)).AssertSuccess();
        for (var i = 0; i < 5; i++)
        {
            (await _entityApiClient.AddSimpleData(Leaner, Name, $"student{i}")).AssertSuccess();
        }

        (await _entityApiClient.AddSimpleData(Leaner, Name, "good-student")).AssertSuccess();
        (await _entityApiClient.AddSimpleData(Leaner, Name, "good-student")).AssertSuccess();

        //get first page
        Assert.Equal(5,(await _entityApiClient.GetEntityList(Leaner, 0, 5)).AssertSuccess().Items.Length);
        //get last page
        Assert.Equal(2,(await _entityApiClient.GetEntityList(Leaner, 5, 5)).AssertSuccess().Items.Length);
    }
}