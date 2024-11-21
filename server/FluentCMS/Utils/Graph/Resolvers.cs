using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using GraphQL;
using GraphQL.Execution;
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
            return await queryService.OneWithAction(GetQuery(context, entityName), fields, Trim(context.Arguments));
        });
    }

    public static IFieldResolver GetListResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record[]>(async context =>
        {
            var fields = context.SubFields!.Values.Select(x => x.Field);
            return await queryService.ListWithAction(GetQuery(context, entityName), fields, Trim(context.Arguments));
        });
    }

    private static Dictionary<string,ArgumentValue> Trim(IDictionary<string, ArgumentValue>? arguments)
    {
        var ret = new Dictionary<string, ArgumentValue>();
        if (arguments is null)
        {
            return ret;
        }

        foreach (var (key, value) in arguments)
        {
            if (value.Value is not null)
            {
                
                ret[key] = value;
            }
        }
        return ret;
    }
    
    private static Query GetQuery(IResolveFieldContext context, string entityName)
    {
        var raw = context.Document.Source.ToString();
        var queryName = "";
        if (context.ExecutionContext.Operation.Name is not null)
        {
            queryName = context.ExecutionContext.Operation.Name.StringValue;
        }
        return new Query(queryName, entityName, 0, raw, [], []);
    }
}