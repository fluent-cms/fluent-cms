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
        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
    }


    [Fact]
    public async Task EntityRetrieve()
    {
        await _accountApiClient.Login();
        await _schemaApiClient.AddSimpleEntity(Leaner, Name);
        for (var i = 0; i < 5; i++)
        {
            await _entityApiClient.AddSimpleData(Leaner, Name, $"student{i}");
        }

        await _entityApiClient.AddSimpleData(Leaner, Name, "good-student");
        await _entityApiClient.AddSimpleData(Leaner, Name, "good-student");

        //get first page
        await _entityApiClient.GetEntityList(Leaner, 0, 5, 7, 5);
        //get last page
        await _entityApiClient.GetEntityList(Leaner, 5, 5, 7, 2);
    }
}