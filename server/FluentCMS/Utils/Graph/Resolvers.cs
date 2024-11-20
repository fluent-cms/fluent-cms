using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using GraphQL;
using GraphQL.Resolvers;

namespace FluentCMS.Utils.Graph;

public static class Resolvers
{
    public static readonly IFieldResolver ValueResolver = new FuncFieldResolver<object>(context =>
    {
        if (context.Source is Record record)
        {
            return record[context.FieldDefinition.Name];
        }

        return null;
    });

    private static ArgumentKeyValueProvider[] GetInputs(this IResolveFieldContext context) =>
        [..context.Arguments?.Where(x=>x.Value.Value is not null)
            .Select(x=> new ArgumentKeyValueProvider(x.Key,x.Value))??[]];

    public static IFieldResolver GetSingleResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record>(async context =>
        {
            var fields = context.SubFields!.Values.Select(x => x.Field);
            return await queryService.OneWithAction(entityName, fields, context.GetInputs());
        });
    }

    public static IFieldResolver GetListResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record[]>(async context =>
        {
            var fields = context.SubFields!.Values.Select(x => x.Field);
            return await queryService.ListWithAction(entityName, fields, context.GetInputs());
        });
    }
}