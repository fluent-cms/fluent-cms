using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.ResultExt;
namespace FluentCMS.Blog.Tests;
public class BasicFlowTest
{
    private const string Tutor = "basic_test_tutor";
    private const string Leaner = "basic_test_leaner";
    private const string Class = "basic_test_class";
    private const string Name = "name";



    private readonly SchemaApiClient _schemaApiClient ;
    private readonly AccountApiClient _accountApiClient ;
    private readonly EntityApiClient _entityApiClient;

    public BasicFlowTest()
    {
        Util.SetTestConnectionString();

        WebAppClient<Program> webAppClient = new();
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task BasicFlow()
    {
        (await _accountApiClient.EnsureLogin()).Ok();
        (await _schemaApiClient.GetTopMenuBar()).Ok();
        (await _schemaApiClient.GetAll("")).Ok();

        (await _schemaApiClient.EnsureSimpleEntity(Leaner, Name)).Ok();
        (await _entityApiClient.Insert(Leaner, Name, "Bob")).Ok();

        (await _schemaApiClient.EnsureSimpleEntity(Class, Name, Tutor, Leaner)).Ok();
        (await _entityApiClient.InsertWithLookup(Class, Name, "class1", Tutor, 1)).Ok();

        (await _entityApiClient.AddJunctionData(Class, Leaner, 1, 1)).Ok();
        Assert.True(1 <= (await _entityApiClient.GetJunctionData(Class, Leaner, false,1)).Ok().Items.Length);
    }
}