using FluentCMS.Cms.Services;
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

    public static IFieldResolver GetSingleResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record>(async context =>
        {
            var fields = context.SubFields!.Values.Select(x => x.Field);
            return await queryService.OneWithAction(entityName, fields);
        });
    }

    public static IFieldResolver GetListResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record[]>(async context =>
        {
            var fields = context.SubFields!.Values.Select(x => x.Field);
            return await queryService.ListWithAction(entityName, fields);
        });
    }
}