using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public interface IAttributeResolver
{
    public bool GetAttrVal(Attribute attribute, string v, out object? value);
    public Task<Result<AttributeVector>> GetAttrVector(LoadedEntity entity, string fieldName);
}