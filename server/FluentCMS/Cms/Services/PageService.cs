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
    public async Task<string> GetDetail(string pageName, string key,  CancellationToken cancellationToken)
    {
        var schema = NotNull(await schemaService.GetByNamePrefixDefault(pageName + "/{", SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        var paramName = schema.Name.Split("/").Last();
        var data =await GetSingle(page.Query, page.QueryString,paramName,key, cancellationToken);
        return await RenderPage(data, page, cancellationToken);
    }

    public async Task<string> Get(string pageName,  CancellationToken cancellationToken)
    {
        var schema = NotNull(await schemaService.GetByNameDefault(pageName, SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        var data = new Dictionary<string, object>();
        return await RenderPage(data, page!, cancellationToken);
    }

    private async Task<string> RenderPage(Record data, Page page,CancellationToken cancellationToken)
    {
         var doc = new HtmlDocument();
         doc.LoadHtml(page!.Html);
         var listNodes = CheckResult(doc.LoadMultipleRecordNode());
         ReplaceMultipleRecordNode(listNodes);
         await AttachListData(data, listNodes, cancellationToken);
        
         var template = Handlebars.Compile(doc.DocumentNode.OuterHtml);
         var html = template(data); 
         return RenderHtml(page.Title, html,page.Css);       
    }

    private void ReplaceMultipleRecordNode(MultipleRecordNode[] listNodes)
    {
        foreach (var listNode in listNodes)
        {
            listNode.HtmlNode.InnerHtml = "{{#each " + listNode.Field+ "}}" + listNode.HtmlNode.InnerHtml + "{{/each}}";
        }
    }

    private async Task<Record> GetSingle(string query, string qs, string paramName, string paramValue, CancellationToken cancellationToken)
    {
        qs = qs.Replace(paramName, paramValue);
        var queryDictionary = QueryHelpers.ParseQuery(qs);
        return await queryService.One(query,queryDictionary, cancellationToken);
    }

    private async Task AttachListData(Record data, MultipleRecordNode[] listNodes, CancellationToken cancellationToken)
    {
        foreach (var multipleRecordNode in listNodes)
        {
            if (!multipleRecordNode.MultipleQuery.IsSuccess) continue;
            var queryInfo = multipleRecordNode.MultipleQuery.Value;
            var pagination = new Pagination { Offset = queryInfo.Offset, Limit = queryInfo.Limit };
            var queryDictionary = QueryHelpers.ParseQuery(queryInfo.Qs);
            var records = await queryService.Many(queryInfo.Query, pagination, queryDictionary, cancellationToken);
            data[multipleRecordNode.Field] = records;
        }
    }

    private static string RenderHtml(string title,string body,  string css)
    {
        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <title>{{title}}</title>
                     <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/base.min.css">
                     <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/components.min.css">
                     <link rel="stylesheet" href="https://unpkg.com/@tailwindcss/typography@0.1.2/dist/typography.min.css">
                     <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/utilities.min.css">
                     <style>
                     {{css}}
                     </style>
                     <link rel="icon" href="/favicon.ico" type="image/x-icon">
                     <style>
                     /* General styles for the button */
                 .edit-button {
                     position: fixed;
                     top: 10px;
                     right: 10px;
                     background-color: #007bff;
                     color: #fff;
                     border: none;
                     padding: 10px 20px;
                     border-radius: 5px;
                     cursor: pointer;
                     font-size: 16px;
                     transition: background-color 0.3s ease;
                 }
                 
                 /* Hover effect for the button */
                 .edit-button:hover {
                     background-color: #0056b3;
                 }
                     </style>
                 </head>
                 {{body}}
                 </html>
                 """;
    }
}

