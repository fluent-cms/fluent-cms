using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public sealed class StringClause : InputObjectGraphType
{
    public StringClause()
    {
        Name = "StringClause";
        
        foreach (var se in Matches.SingleStr)
        {
            AddField(new FieldType
            {
                Name = se,
                Type = typeof(StringGraphType)
            });
        }
        foreach (var se in Matches.MultiStr)
        {
            AddField(new FieldType
            {
                Name = se,
                Type = typeof(ListGraphType<StringGraphType>)
            });
        }
        this.AddClauseCommonField();
    }
}
public sealed class LogicalOperatorEnum : EnumerationGraphType
{
    public LogicalOperatorEnum()
    {
        Name = "LogicalOperatorEnum";
        Add(new EnumValueDefinition(LogicalOperators.And,LogicalOperators.And));
        Add(new EnumValueDefinition(LogicalOperators.Or,LogicalOperators.Or));
    }
}
public sealed class DateClause : InputObjectGraphType
{
    public DateClause()
    {
        Name = "DateClause";
        
        foreach (var se in Matches.SingleDate)
        {
            AddField(new FieldType
            {
                Name = se,
                Type = typeof(DateGraphType)
            });
        }
        foreach (var se in Matches.MultiDate)
        {
            AddField(new FieldType
            {
                Name = se,
                Type = typeof(ListGraphType<DateGraphType>)
            });
        }
        this.AddClauseCommonField();
    }
}
public sealed class IntClause : InputObjectGraphType
{
    public IntClause()
    {
        Name = "IntClause";
        
        foreach (var se in Matches.SingleInt)
        {
            AddField(new FieldType
            {
                Name = se,
                Type = typeof(IntGraphType)
            });
        }

        foreach (var se in Matches.MultiInt)
        {
             AddField(new FieldType
             {
                 Name = se,
                 Type = typeof(ListGraphType<IntGraphType>)
             });
        }
        this.AddClauseCommonField();
    }
}

public static class ClauseExt
{
    public static void AddClauseCommonField(this InputObjectGraphType t)
    {
        t.AddField(new FieldType
        {
            Name = FilterConstants.OperatorKey ,
            Type = typeof(LogicalOperatorEnum),
        });
    }
}
