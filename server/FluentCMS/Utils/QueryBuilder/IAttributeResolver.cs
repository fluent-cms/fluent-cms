using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public interface IAttributeValueResolver
{
    public bool ResolveVal(Attribute attr, string v, out object? value);
}

public interface IEntityVectorResolver
{
    public Task<Result<AttributeVector>> ResolveVector(LoadedEntity entity, string fieldName);
}