namespace FluentCMS.Utils.Graph;

public class CmsSchema: GraphQL.Types.Schema
{
    public CmsSchema(IServiceProvider services): base(services)
    {
        Query = services.GetRequiredService<CmsQuery>();
    }
}