using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public record GraphInfo(Entity Entity, ObjectGraphType SingleType, ListGraphType ListType);
public sealed class GraphQuery : ObjectGraphType
{
    public GraphQuery(IEntitySchemaService entitySchemaService, IQueryService queryService)
    {
        if (!entitySchemaService.TryGetCachedSchema(out var entities))
        {
            return;
        }
        
        var dict = new Dictionary<string, GraphInfo>();
        
        foreach (var entity in entities)
        {
            var t = FieldTypes.PlainType(entity);
            dict[entity.Name] = new GraphInfo(entity, t, new ListGraphType(t));
        }
        
        foreach (var entity in entities)
        {
            FieldTypes.SetCompoundType(entity,dict);
        }

        foreach (var entity in entities)
        {
            var graphInfo = dict[entity.Name];
            AddField(new FieldType
            {
                Name = entity.Name,
                ResolvedType = graphInfo.SingleType,
                Resolver = Resolvers.GetSingleResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    ..Args.FilterArgs(entity), 
                    Args.FilterExprArg
                ])
            });
            
            AddField(new FieldType
            {
                Name = entity.Name + "List",
                ResolvedType = graphInfo.ListType,
                Resolver = Resolvers.GetListResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    Args.OffsetArg,
                    Args.LimitArg,
                    Args.SortArg(entity),
                    ..Args.FilterArgs(entity), 
                    Args.SortExprArg,
                    Args.FilterExprArg
                ])
            });
        }
    }
}