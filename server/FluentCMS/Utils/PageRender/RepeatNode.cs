using HtmlAgilityPack;

namespace FluentCMS.Utils.PageRender;

public enum PaginationType
{
    None,
    Button,
    InfiniteScroll
}

public record Repeat(
    PaginationType PaginationType,
    string Field,
    string Query,
    string QueryString,
    int Offset,
    int Limit
);


public record RepeatNode(
    HtmlNode HtmlNode,
    Repeat Repeat
);

public static class RepeatNodeExtensions
{
    public static PartialToken ToPartialToken(this RepeatNode node, string page, string first ="", string last = "")
    {
        return new PartialToken(
            NodeId: node.HtmlNode.Id,
            Repeat: node.Repeat,
            Page: page,
            First: "",
            Last: ""
        );
    }
}
