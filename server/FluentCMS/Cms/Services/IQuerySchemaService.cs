using FluentCMS.Graph;
using FluentCMS.Utils.QueryBuilder;
using Schema = FluentCMS.Cms.Models.Schema;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> ByGraphQlRequest(GraphQlRequestDto qlRequest);
    Task<LoadedQuery> ByNameAndCache(string name, CancellationToken token);
    Task Delete(Schema schema, CancellationToken token);
    string GraphQlClientUrl();

}