namespace FluentCMS.Utils.Graph;

public class Schema: GraphQL.Types.Schema
{
    public Schema(IServiceProvider services): base(services)
    {
        Query = services.GetRequiredService<Query>();
    }
}