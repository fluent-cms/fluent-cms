using System.Collections.Immutable;

namespace FluentCMS.Utils.QueryBuilder;

public interface IValueProvider
{
    string Name();
    bool Vals(out ImmutableArray<string> values);
    bool Pairs(out ImmutableArray<(string,object)> pairs);
}