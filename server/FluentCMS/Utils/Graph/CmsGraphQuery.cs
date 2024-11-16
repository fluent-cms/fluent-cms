using System.Collections.Immutable;
using FluentCMS.Cms.Services;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public sealed class CmsGraphQuery : ObjectGraphType
{
    public CmsGraphQuery(ISchemaService schemaService, IQueryService queryService)
    {
        if (!schemaService.GetCachedSchema(SchemaType.Entity,out var schemas))
        {
            return;
        }
        var entities = schemas
            .Where(x=>x.Settings.Entity is not null)
            .Select(x=>x.Settings.Entity).ToImmutableArray();
        
        var dict = new Dictionary<string, ObjectGraphType>();
        foreach (var entity in entities)
        {
            dict[entity!.Name] = entity.GetPlainGraphType();
        }
        
        foreach (var entity in entities)
        {
            entity!.LoadCompoundGraphType(dict);
        }
        
        foreach (var entity in entities)
        {
            AddField(new FieldType
            {
                Name =entity!.Name, 
                ResolvedType = dict[entity.Name],
                Resolver = new FuncFieldResolver<Record[]>(async context =>
                {
                    var fields = context.SubFields is null ?[]:context.SubFields.Values.Select(x => x.Field);
                    var items = await queryService.Query(context.FieldDefinition.Name,fields);
                    return items; 
                })
            });
        }
    }
}