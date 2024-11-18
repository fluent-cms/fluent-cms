using System.Collections.Immutable;
using FluentResults;
using OneOf;

namespace FluentCMS.Utils.QueryBuilder;

public record ValueWrapper(OneOf<string, ImmutableArray<string>, ImmutableArray<(string, object)>, List<IError>> Val);
public interface IValueProvider
{
    string Name();
    ValueWrapper Val();
}