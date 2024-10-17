using FluentCMS.Cms.Services;

namespace FluentCMS.Tests.Services;

public class SchemaServiceTests :ServiceTestBase
{
    private readonly SchemaService _schemaService;
    
    public SchemaServiceTests()
    {
        _schemaService = new SchemaService(SqliteDefinitionExecutor, KateQueryExecutor,HookRegistry, ServiceProvider);
    }
    [Fact]
    public async Task AddSchemaTable_NoException()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureSchemaTable();
    }
    [Fact]
    public async Task AddTopMenuBar_Success()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        var menus = await _schemaService.GetByNameDefault(SchemaName.TopMenuBar,SchemaType.Menu);
        Assert.NotNull(menus);
    }
}