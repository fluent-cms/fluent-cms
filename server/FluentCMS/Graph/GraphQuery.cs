using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Graph;

public record GraphInfo(Entity Entity, ObjectGraphType SingleType, ListGraphType ListType);
public sealed class GraphQuery : ObjectGraphType
{
    public GraphQuery(IEntitySchemaService entitySchemaService, IQueryService queryService)
    {
        var entities = entitySchemaService.AllEntities().GetAwaiter().GetResult();
        var graphMap = new Dictionary<string, GraphInfo>();
        
        foreach (var entity in entities)
        {
            var t = FieldTypes.PlainType(entity);
            graphMap[entity.Name] = new GraphInfo(entity, t, new ListGraphType(t));
        }
        
        foreach (var entity in entities)
        {
            FieldTypes.SetCompoundType(entity,graphMap);
        }

        foreach (var entity in entities)
        {
            var graphInfo = graphMap[entity.Name];
            AddField(new FieldType
            {
                Name = entity.Name,
                ResolvedType = graphInfo.SingleType,
                Resolver = Resolvers.GetSingleResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    ..Args.FilterArgs(entity,graphMap), 
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
                    ..Args.FilterArgs(entity,graphMap), 
                    Args.SortExprArg,
                    Args.FilterExprArg
                ])
            });
        }
    }
}