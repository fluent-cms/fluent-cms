using FluentCMS.Utils.ApiClient;

namespace FluentCMS.IntegrationTests;
public class BasicFlowTest
{
    private const string Tutor = "tutor";
    private const string Leaner = "leaner";
    private const string Class = "class";
    private const string Name = "name";



    private readonly SchemaApiClient _schemaApiClient ;
    private readonly AccountApiClient _accountApiClient ;
    private readonly EntityApiClient _entityApiClient;

    public BasicFlowTest()
    {
        WebAppClient<Program> webAppClient = new();
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task BasicFlow()
    {
        await _accountApiClient.EnsureLogin();
        (await _schemaApiClient.GetTopMenuBar()).AssertSuccess();
        (await _schemaApiClient.GetAll("")).AssertSuccess();

        (await _schemaApiClient.EnsureSimpleEntity(Tutor, Name)).AssertSuccess();
        (await _entityApiClient.AddSimpleData(Tutor, Name, "Tom")).AssertSuccess();
        (await _entityApiClient.UpdateSimpleData(Tutor, 1, Name, "TomUpdate")).AssertSuccess();
        Assert.Equal("TomUpdate",(await _entityApiClient.GetEntityValue(Tutor, 1)).AssertSuccess()[Name].GetString());

        (await _schemaApiClient.EnsureSimpleEntity(Leaner, Name)).AssertSuccess();
        (await _entityApiClient.AddSimpleData(Leaner, Name, "Bob")).AssertSuccess();

        (await _schemaApiClient.EnsureSimpleEntity(Class, Name, Tutor, Leaner)).AssertSuccess();
        (await _entityApiClient.AddDataWithLookup(Class, Name, "class1", Tutor, 1)).AssertSuccess();

        (await _entityApiClient.AddCrosstableData(Class, Leaner, 1, 1)).AssertSuccess();
        Assert.True(1 <= (await _entityApiClient.CrossTable(Class, Leaner, false,1)).AssertSuccess().Items.Length);
    }
}