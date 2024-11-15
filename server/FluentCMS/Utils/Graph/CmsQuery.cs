using FluentCMS.Cms.Services;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public sealed class CmsQuery : ObjectGraphType
{
    public CmsQuery(IEntitySchemaService schemaService)
    {
        if (!schemaService.GetLoadedEntities(out var entities))
        {
            return;
        }
        foreach (var entity in entities)
        {
            AddField(new FieldType {Name =entity.Name, ResolvedType = entity.EntityGraphType() });
        }
    }
}