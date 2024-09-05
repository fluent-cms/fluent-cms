using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;
public sealed class PageService(ISchemaService schemaService,IQueryService queryService):IPageService
{
    public async Task<string> GetBySlug(string pageName, string slug,  CancellationToken cancellationToken)
    {
        var page = NotNull(await schemaService.GetByNameDefault(Page.SinglePageName(pageName), SchemaType.Page, cancellationToken))
            .ValOrThrow($"can not find page {pageName}").Settings.Page;
        var doc = new HtmlDocument();
        doc.LoadHtml(page!.Html);
        StrNotEmpty(page.Query).ValOrThrow($"can not find query of page [{pageName}]");
        var data =await GetSingle(page.Query, page.QueryString,slug, cancellationToken);
        await ReplaceMultipleRecordNode(doc, cancellationToken);
        var template = Handlebars.Compile(doc.DocumentNode.OuterHtml);
        var html = template(data);
        return RenderHtml(html, page.Css);
    }

    public async Task<string> Get(string pageName,  CancellationToken cancellationToken)
    {
        var page = NotNull(await schemaService.GetByNameDefault(pageName, SchemaType.Page, cancellationToken))
            .ValOrThrow($"can not find page {pageName}").Settings.Page;
        var doc = new HtmlDocument();
        doc.LoadHtml(page!.Html);
        await ReplaceMultipleRecordNode(doc, cancellationToken);
        return RenderHtml(doc.DocumentNode.OuterHtml,page.Css);
    }

    private async Task ReplaceMultipleRecordNode(HtmlDocument doc, CancellationToken cancellationToken)
    {
        var listNodes = CheckResult(doc.LoadMultipleRecordNode());
        foreach (var listNode in listNodes)
        {
            var template = Handlebars.Compile(listNode.HtmlNode.OuterHtml);
            var html = "";
            var data = await GetListData(listNode.MultipleQuery, cancellationToken);
            html = template(data);
            listNode.HtmlNode.ParentNode.ReplaceChild(HtmlNode.CreateNode(html), listNode.HtmlNode);
        }
    }

    private async Task<Record> GetSingle(string query, string qs, string slug, CancellationToken cancellationToken)
    {
        qs = qs.Replace("{slug}", slug);
        var queryDictionary = QueryHelpers.ParseQuery(qs);
        return await queryService.One(query,queryDictionary, cancellationToken);
    }

    private async Task<MultipleRecordData> GetListData(MultipleRecordQuery queryInfo,CancellationToken cancellationToken)
    {
        var pagination = new Pagination { Offset = queryInfo.Offset, Limit = queryInfo.Limit };
        var queryDictionary = QueryHelpers.ParseQuery(queryInfo.Qs);
        var records = await queryService.Many(queryInfo.Query, pagination, queryDictionary, cancellationToken);
        return new MultipleRecordData(records);
    }

    private static string RenderHtml(string body, string css)
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
}

