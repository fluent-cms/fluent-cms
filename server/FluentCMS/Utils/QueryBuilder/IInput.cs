using System.Collections.Immutable;
using FluentResults;
using OneOf;

namespace FluentCMS.Utils.QueryBuilder;

public interface IInput
{
    string Name();
    OneOf<string,ImmutableArray<(string,object)>,List<IError>> Val();
}