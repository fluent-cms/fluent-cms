using System.Collections.Immutable;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public sealed class Query : ObjectGraphType
{
    public Query(ISchemaService schemaService, IQueryService queryService)
    {
        if (!schemaService.GetCachedSchema(SchemaType.Entity,out var schemas))
        {
            return;
        }
        var entities = schemas
            .Where(x=>x.Settings.Entity is not null)
            .Select(x=>x.Settings.Entity).ToImmutableArray();
        
        var singleDict = new Dictionary<string, ObjectGraphType>();
        var listDict = new Dictionary<string, ListGraphType>();
        var entityDict = new Dictionary<string, Entity>();
        
        foreach (var entity in entities)
        {
            var t = entity!.GetPlainGraphType();
            singleDict[entity!.Name] = t;
            listDict[entity.Name] = new ListGraphType(t);
            entityDict[entity.Name] = entity;
        }
        
        foreach (var entity in entities)
        {
            entity!.LoadCompoundGraphType(singleDict,listDict,entityDict);
        }

        foreach (var entity in entities)
        {
            AddField(new FieldType
            {
                Name = entity!.Name,
                ResolvedType = singleDict[entity.Name],
                Resolver = Resolvers.GetSingleResolver(queryService,entity.Name),
                Arguments = entity.GetArgument(false)
            });
            
            AddField(new FieldType
            {
                Name = entity.Name + "List",
                ResolvedType = listDict[entity.Name],
                Resolver = Resolvers.GetListResolver(queryService, entity.Name),
                Arguments = entity.GetArgument(true)
            });
        }
    }
}