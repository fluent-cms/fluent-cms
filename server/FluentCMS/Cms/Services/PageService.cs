using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
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
        return await RenderPage(page, data, qsDictionary, cancellationToken);
    }

    public async Task<string> Get(string pageName, Dictionary<string, StringValues> qsDictionary,
        CancellationToken cancellationToken)
    {
        var schema = NotNull(await schemaService.GetByNameDefault(pageName, SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        var data = new Dictionary<string, object>();
        return await RenderPage(page, data, qsDictionary, cancellationToken);
    }

    private async Task<string> RenderPage(Page page, Record data, Dictionary<string,StringValues> qsDictionary, CancellationToken cancellationToken )
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(page.Html);

        var repeatingNodes = CheckResult(doc.GetRepeatingNodes(qsDictionary));
        repeatingNodes.SetLoopAndPagination();

        await LoadData(data, repeatingNodes, cancellationToken);
        SetDataPagination(page.Name, data, repeatingNodes);
        
        var title = GetTitle(page.Title, data);
        var body = GetBody(doc, data);
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
        var result = await PrepareData();
        SetResultPagination(result,token);
        var data = new Dictionary<string, object> { { token.Field, result } };
        return template(data);

        HtmlNode ParseNode()
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(page.Html);
            var node = doc.GetElementbyId(token.NodeId);
            node.AddLoop(token.Field + ".items");
            node.AddCursor(token.Field);
            if (token.PaginationType == PaginationType.InfiniteScroll) node.AddPagination(token.Field);
            return node;

        }

        async Task<RecordQueryResult> PrepareData()
        {
            var cursor = new Cursor { First = token.First, Last = token.Last };
            var pagination = new Pagination { Offset = token.Offset, Limit = token.Limit };
            return await queryService.List(token.Query, cursor, pagination,  QueryHelpers.ParseQuery(token.Qs), cancellationToken);
        }
    }

    private async Task LoadData(Record data,RepeatNode[] repeatNodes, CancellationToken cancellationToken)
    {
        foreach (var repeatNode in repeatNodes)
        {
            if (repeatNode.MultipleQuery is null)
            {
                continue;
            }

            var pagination = new Pagination
                { Offset = repeatNode.MultipleQuery.Offset, Limit = repeatNode.MultipleQuery.Limit };
            var result = await queryService.List(repeatNode.MultipleQuery.Query, new Cursor(), pagination,
                repeatNode.MultipleQuery.Qs, cancellationToken);
            data[repeatNode.Field] = result;
        }
    }
    
    private  string GetBody(HtmlDocument doc, Record data)
    {
        var html = doc.DocumentNode.FirstChild.InnerHtml;
        var template = Handlebars.Compile(html);
        return template(data);
    }

    private static void SetResultPagination(RecordQueryResult result, PartialToken token)
    {
        result.Last = string.IsNullOrEmpty(result.Last)  ? "" : (token with { Last = result.Last, First = ""}).ToString();
        result.First = string.IsNullOrEmpty(result.First) ? "" : (token with { First = result.First, Last = ""}).ToString();
    }

    private static void SetDataPagination(string pageName, Record data, RepeatNode[] repeatNodes)
    {
        foreach (var repeatNode in repeatNodes)
        {
            if (repeatNode.MultipleQuery is not null && data.TryGetValue(repeatNode.Field, out var value) &&
                value is RecordQueryResult result)
            {
                var token = new PartialToken(pageName, repeatNode.HtmlNode.Id, repeatNode.Field,
                    repeatNode.PaginationType,
                    repeatNode.MultipleQuery!.Query, repeatNode.MultipleQuery.Offset, repeatNode.MultipleQuery.Limit,
                    "", "",
                    GetQueryString(repeatNode.MultipleQuery.Qs));
                SetResultPagination(result, token);
            }
        }
    }

    private static string GetQueryString(Dictionary<string, StringValues> queryParams)
    {
        return  string.Join("&", queryParams.Select(kvp =>
            string.Join("&", kvp.Value.Select(value => $"{kvp.Key}={value}"))));
    }
    
    private static string GetTitle( string title,Record data) => Handlebars.Compile(title)(data);
}

