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
        await _accountApiClient.Login();
        await _schemaApiClient.GetTopMenuBar();
        await _schemaApiClient.GetAll("");

        await _schemaApiClient.AddSimpleEntity(Tutor, Name);
        await _entityApiClient.AddSimpleData(Tutor, Name, "Tom");
        await _entityApiClient.UpdateSimpleData(Tutor, 1, Name, "TomUpdate");
        await _entityApiClient.GetEntityValue(Tutor, 1, Name, "TomUpdate");

        await _schemaApiClient.AddSimpleEntity(Leaner, Name);
        await _entityApiClient.AddSimpleData(Leaner, Name, "Bob");

        await _schemaApiClient.AddSimpleEntity(Class, Name, Tutor, Leaner);
        await _entityApiClient.AddDataWithLookup(Class, Name, "class1", Tutor, 1);

        await _entityApiClient.AddCrosstableData(Class, Leaner, 1, 1);
        await _entityApiClient.CrossTableCount(Class, Leaner, false, 1, 1);
    }
}