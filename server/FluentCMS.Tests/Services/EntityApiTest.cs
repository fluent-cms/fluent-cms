using FluentCMS.Cms.Services;

namespace FluentCMS.Tests.Services;

public class EntityServiceTest  :ServiceTestBase
{
    private EntityService _entityService;
    public EntityServiceTest()
    {
        var schemaService = new SchemaService(SqliteDefinitionExecutor, KateQueryExecutor,HookRegistry, ServiceProvider);
        var entitySchemaService = new EntitySchemaService(schemaService, SqliteDefinitionExecutor);

        _entityService = new EntityService(ServiceProvider, KateQueryExecutor, schemaService, entitySchemaService,
            HookRegistry);
    }
}