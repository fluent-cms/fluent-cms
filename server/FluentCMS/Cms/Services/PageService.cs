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
public sealed class PageService(ISchemaService schemaService,IQueryService queryService):IPageService
{
    private static string RemoveBrace(string fullRouterParamName) => fullRouterParamName[1..^1]; 
    
    public async Task<string> GetDetail(string pageName, string paramValue,  Dictionary<string,StringValues> qsDictionary, CancellationToken cancellationToken)
    {
        var schema = NotNull(await schemaService.GetByNamePrefixDefault(pageName + "/{", SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        var bracedName = schema.Name.Split("/").Last();
        var routerName = RemoveBrace(bracedName);
        if (bracedName != paramValue)
        {
            qsDictionary[routerName] = paramValue;
        }
        
        var data = await queryService.One(page.Query, qsDictionary, cancellationToken);
        var body = await RenderBody(data, page, qsDictionary, cancellationToken);
        var title = RenderTitle(data, page);
        return RenderHtml(title, body, page.Css);
    }

    public async Task<string> Get(string pageName, Dictionary<string,StringValues> qsDictionary,  CancellationToken cancellationToken)
    {
        var schema = NotNull(await schemaService.GetByNameDefault(pageName, SchemaType.Page, cancellationToken))
            .ValOrThrow($"Can not find page [{pageName}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        Record data = new Dictionary<string, object>();
        var body = await RenderBody(new Dictionary<string, object>(), page, qsDictionary,cancellationToken);
        var title = RenderTitle(data, page);
        return RenderHtml(title, body, page.Css);
    }
    
    private string RenderTitle(Record data, Page page)
    {
         var template = Handlebars.Compile(page.Title);
         return template(data); 
    }

    private async Task<string> RenderBody(Record data, Page page, Dictionary<string,StringValues> qsDict, CancellationToken cancellationToken)
    {
         var doc = new HtmlDocument();
         doc.LoadHtml(page.Html);
         var listNodes = CheckResult(doc.GetMultipleRecordNode());
         AddLoop(listNodes);
         await AttachListData(data, listNodes,qsDict,  cancellationToken);
        
         var template = Handlebars.Compile(doc.DocumentNode.OuterHtml);
         return template(data); 
    }


    private async Task AttachListData(Record data, MultipleRecordNode[] listNodes, Dictionary<string,StringValues> qsDict, CancellationToken cancellationToken)
    {
        foreach (var multipleRecordNode in listNodes)
        {
            if (!multipleRecordNode.MultipleQuery.IsSuccess) continue;
            var queryInfo = multipleRecordNode.MultipleQuery.Value;
            var pagination = new Pagination { Offset = queryInfo.Offset, Limit = queryInfo.Limit };

            var dict = new Dictionary<string, StringValues>(qsDict);//make a copy
            foreach (var (key, value) in QueryHelpers.ParseQuery(queryInfo.Qs))
            {
                dict[key] = value;
            }
            var records = await queryService.Many(queryInfo.Query, pagination, dict, cancellationToken);
            data[multipleRecordNode.Field] = records;
        }
    }
    private static void AddLoop(MultipleRecordNode[] listNodes)
    {
        foreach (var listNode in listNodes)
        {
            listNode.HtmlNode.InnerHtml = "{{#each " + listNode.Field+ "}}" + listNode.HtmlNode.InnerHtml + "{{/each}}";
        }
    }

    private static string RenderHtml(string title,string body,  string css)
    {
        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="UTF-8">
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

