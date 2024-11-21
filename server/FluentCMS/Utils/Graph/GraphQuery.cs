using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public record GraphInfo(Entity Entity, ObjectGraphType SingleType, ListGraphType ListType);
public sealed class GraphQuery : ObjectGraphType
{
    public GraphQuery(ISchemaService schemaService, IQueryService queryService)
    {
        if (!schemaService.GetCachedSchema(SchemaType.Entity,out var schemas))
        {
            return;
        }
        var entities = schemas
            .Where(x=>x.Settings.Entity is not null)
            .Select(x=>x.Settings.Entity).ToArray();
        
        var dict = new Dictionary<string, GraphInfo>();
        
        foreach (var entity in entities)
        {
            var t = entity!.PlainType();
            dict[entity!.Name] = new GraphInfo(entity, t, new ListGraphType(t));
        }
        
        foreach (var entity in entities)
        {
            entity!.SetCompoundType(dict);
        }

        foreach (var entity in entities)
        {
            var graphInfo = dict[entity!.Name];
            var limitArg = new QueryArgument<IntGraphType>{Name = QueryConstants.LimitKey};
            AddField(new FieldType
            {
                Name = entity.Name,
                ResolvedType = graphInfo.SingleType,
                Resolver = Resolvers.GetSingleResolver(queryService,entity.Name),
                Arguments = new QueryArguments([
                    ..entity.FilterArgs(), 
                    ArgumentTypes.FilterExpr()
                ])
            });
            
            AddField(new FieldType
            {
                Name = entity.Name + "List",
                ResolvedType = dict[entity.Name].ListType,
                Resolver = Resolvers.GetListResolver(queryService, entity.Name),
                Arguments = new QueryArguments([
                    limitArg,
                    entity.SortArg(),
                    ..entity.FilterArgs(), 
                    ArgumentTypes.SortExpr(), 
                    ArgumentTypes.FilterExpr()
                ])
            });
        }
    }
}