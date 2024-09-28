using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class PageService(ISchemaService schemaService, IQueryService queryService, Renderer renderer)
    : IPageService
{
    private static string RemoveBrace(string fullRouterParamName) => fullRouterParamName[1..^1];

    public async Task<string> GetDetail(string pageName, string paramValue,
        Dictionary<string, StringValues> qsDictionary, CancellationToken cancellationToken)
    {
        var schema =
            NotNull(await schemaService.GetByNamePrefixDefault(pageName + "/{", SchemaType.Page, cancellationToken))
                .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        var bracedName = schema.Name.Split("/").Last();
        var routerName = RemoveBrace(bracedName);
        if (bracedName != paramValue)
        {
            qsDictionary[routerName] = paramValue;
        }

        var data = await queryService.One(page.Query, qsDictionary, cancellationToken);
        var body = await GetBody(data, page, qsDictionary, cancellationToken);
        var title = GetTitle(data, page);
        return renderer.RenderHtml(title, body, page.Css);
    }

    public async Task<string> Get(string pageName, Dictionary<string, StringValues> qsDictionary,
        CancellationToken cancellationToken)
    {
        var schema = NotNull(await schemaService.GetByNameDefault(pageName, SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        Record data = new Dictionary<string, object>();
        var body = await GetBody(new Dictionary<string, object>(), page, qsDictionary, cancellationToken);
        var title = GetTitle(data, page);
        return renderer.RenderHtml(title, body, page.Css);
    }

    public async Task<string> GetPartial(string tokenString, CancellationToken cancellationToken)
    {
        var token = NotNull(PartialToken.Parse(tokenString)).ValOrThrow("Invalid Partial Token");
        var schema = NotNull(await schemaService.GetByNameDefault(token.Page, SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{token.Page}]");
        var page = NotNull(schema.Settings.Page)
            .ValOrThrow($"Can not find page [{token.Page}]");

        var htmlNode = ParseNode();
        var template = Handlebars.Compile(htmlNode.OuterHtml);
        var data = await PrepareData();
        return template(data);

        HtmlNode ParseNode()
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(page.Html);
            var node = doc.GetElementbyId(token.NodeId);
            node.AddLoop(token.Field);
            if (token.PaginationType == PaginationType.InfiniteScroll) node.AddPagination(token.Field);
            return node;

        }

        async Task<Dictionary<string, object>> PrepareData()
        {
            var cursor = new Cursor { First = token.First, Last = token.Last };
            var pagination = new Pagination { Offset = token.Offset, Limit = token.Limit };
            var result = await queryService.List(token.Query, cursor, pagination, token.Qs, cancellationToken);

            result.First = string.IsNullOrWhiteSpace(result.First)
                ? ""
                : (token with { Last = "", First = result.First }).ToString();
            result.Last = string.IsNullOrWhiteSpace(result.Last)
                ? ""
                : (token with { Last = result.Last, First = "" }).ToString();
            return new Dictionary<string, object> { { token.Field, result } };
        }
    }

    private async Task<string> GetBody(Record data, Page page, Dictionary<string, StringValues> qsDict,
        CancellationToken cancellationToken)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(page.Html);
        var listNodes = CheckResult(doc.GetRepeatingNodes(qsDict));
        foreach (var (htmlNode, field, paginationType, _) in listNodes)
        {
            htmlNode.AddLoop(field);
            if (paginationType == PaginationType.InfiniteScroll) htmlNode.AddPagination(field);
        }

        foreach (var repeatNode in listNodes)
        {
            var pagination = new Pagination { Offset = repeatNode.MultipleQuery.Offset, Limit = repeatNode.MultipleQuery.Limit };
            var result = await queryService.List(repeatNode.MultipleQuery.Query, new Cursor(), pagination,
                repeatNode.MultipleQuery.Qs, cancellationToken);
            SetPaginationData(result, page.Name, repeatNode);
            data[repeatNode.Field] = result;
        }
        var template = Handlebars.Compile(doc.DocumentNode.FirstChild.InnerHtml);
        return template(data);
    }

    private static void SetPaginationData(RecordQueryResult result, string page, RepeatNode repeatNode)
    {
        var token = new PartialToken(page, repeatNode.HtmlNode.Id, repeatNode.Field, repeatNode.PaginationType,
            repeatNode.MultipleQuery.Query, repeatNode.MultipleQuery.Offset, repeatNode.MultipleQuery.Limit, "", "",
            repeatNode.MultipleQuery.Qs);

        result.Last = string.IsNullOrWhiteSpace(result.Last) ? "" :(token with{Last = result.Last}).ToString();
        result.First = string.IsNullOrWhiteSpace(result.First) ? "" :(token with{First = result.First}).ToString();
    }

    private static string GetTitle(Record data, Page page) => Handlebars.Compile(page.Title)(data);
}

