using System.Collections.Immutable;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public record AttributeVector(
    string FullPath,
    string TableAlias,
    ImmutableArray<LoadedAttribute> Ancestors,
    LoadedAttribute Attribute);

public static class AttributeVectorConstants
{
    public const string Separator = "_";
}

//reference type make it easier to build a tree
public class AttributeTreeNode(LoadedAttribute? attribute = default)
{
    public LoadedAttribute? Attribute { get;} = attribute;
    public List<AttributeTreeNode> Children { get; } = new();

    public static AttributeTreeNode Parse(IEnumerable<AttributeVector> vectors)
    {
        var root = new AttributeTreeNode();
        foreach (var vector in vectors)
        {
            var current = root;
            foreach (var attribute in vector.Ancestors)
            {
                var find = current.Children.FirstOrDefault(x => x.Attribute?.Field == attribute.Field);
                if (find is null)
                {
                    find = new AttributeTreeNode(attribute);
                    current.Children.Add(find);
                }

                current = find;
            }
        }
        return root;
    }
}