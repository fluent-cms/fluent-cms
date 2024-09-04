using Confluent.Kafka;
using FluentCMS.Services;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;
public class PageService(ISchemaService schemaService,IQueryService queryService):IPageService
{
    public async Task<string> Get(string pageName, Cursor cursor, CancellationToken cancellationToken)
    {
        var page = NotNull(await schemaService.GetByNameDefault(pageName, SchemaType.Page, cancellationToken))
            .ValOrThrow($"can not find page {pageName}").Settings.Page;
        var doc = new HtmlDocument();
        doc.LoadHtml(page!.Html);
        var listNodes = CheckResult(doc.LoadMultipleRecordNode());
        foreach (var listNode in listNodes)
        {
            //to do: cache template by query + node.id
            var template = Handlebars.Compile(listNode.Html);
            var data = await GetListData(listNode.MultipleQuery, cancellationToken);
            data = data.Map(listNode.Mapping);
            var html = template(data);
            listNode.HtmlNode.ParentNode.ReplaceChild(HtmlNode.CreateNode(html), listNode.HtmlNode);
        }
        return RenderHtml(doc.DocumentNode.OuterHtml, page.Css );
    }

    private string RenderHtml(string body, string css)
    {
        return $"""
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/tailwindcss/2.0.2/tailwind.min.css">
    <style>
    {css}
    </style>
</head>
{body}
</html>
""";
    }

    private async Task<MultipleRecordData> GetListData(MultipleRecordQuery queryInfo,CancellationToken cancellationToken)
    {
        var pagination = new Pagination { Offset = queryInfo.Offset, Limit = queryInfo.Limit };
        var queryDictionary = QueryHelpers.ParseQuery(queryInfo.Qs);
        var records = await queryService.Many(queryInfo.Query, pagination, queryDictionary, cancellationToken);
        return new MultipleRecordData(records);
    }
}

