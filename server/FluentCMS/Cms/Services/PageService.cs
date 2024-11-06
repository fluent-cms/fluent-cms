using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class PageService(ISchemaService schemaSvc, IQueryService querySvc, HtmlTemplate template) : IPageService
{
    public async Task<string> GetDetail(string name, string param, QueryArgs args, CancellationToken token)
    {
        //detail page format <pageName>/{<routerName>}, not know the exact page name now, match prefix with '/{'. 
        var ctx = (await GetContext(name+ "/{" , true,token)).ToPageContext();
        args = GetLocalPaginationArgs(ctx, args); 
        
        var routerName = RenderUtil.RemoveBrace(ctx.Page.Name.Split("/").Last());
        args[routerName] = param;
        
        var data = string.IsNullOrWhiteSpace(ctx.Page.Query)
            ? new Dictionary<string, object>()
            : await querySvc.One(ctx.Page.Query, args, token);
        
        return await RenderPage(ctx, data, args, token);
    }

    public async Task<string> Get(string name, QueryArgs args, CancellationToken token)
    {
        var ctx = await GetContext(name , false, token);
        return await RenderPage(ctx.ToPageContext(),  new Dictionary<string, object>(), args, token);
    }

    private async Task<string> RenderPage(PageContext ctx, Record data, QueryArgs args, CancellationToken token)
    {
        await LoadRelatedData(ctx.Page.Name, data,args, ctx.Nodes, token);
        TagPagination(ctx,data);
        
        ctx.Nodes.SetLoopAndPagination();
        var title = RenderUtil.GetTitle(ctx.Page.Title, data);
        var body = RenderUtil.GetBody(ctx.HtmlDocument, data);
        return template.RenderHtml(title, body, ctx.Page.Css);
    }

    public async Task<string> GetPartial(string patialToken, CancellationToken token)
    {
        var partialToken = NotNull(PartialToken.Parse(patialToken)).ValOrThrow("Invalid Partial Token");
        var ctx = (await GetContext(partialToken.Page, false, token)).ToPartialContext(partialToken.NodeId);
        ctx.Node.SetLoopAndPagination(partialToken.Repeat.Field, partialToken.Repeat.PaginationType);

        var cursor = new Span(partialToken.First, partialToken.Last);
        var args = QueryHelpers.ParseQuery(partialToken.Repeat.QueryString);

        Record[] items;
        if (!string.IsNullOrWhiteSpace(partialToken.Repeat.Query))
        {
            var pagination = new Pagination(0, partialToken.Repeat.Limit);
            items = await querySvc.List(partialToken.Repeat.Query, cursor, pagination, args, token);
        }
        else
        {
            items = await querySvc.Partial(ctx.Page.Query!, partialToken.Repeat.Field, cursor, partialToken.Repeat.Limit, args, token);
        }

        var data = new Dictionary<string, object>
        {
            [partialToken.Repeat.Field] = items
        };
        TagPagination(data, items, partialToken);
        
        var render = Handlebars.Compile(ctx.Node.OuterHtml);
        return render(data);
    }

    
    private static QueryArgs GetLocalPaginationArgs(PageContext ctx,QueryArgs args)
    {
        var ret = new QueryArgs(args);
        foreach (var node in ctx.Nodes.Where(x => 
                string.IsNullOrWhiteSpace(x.Repeat.Query) && (x.Repeat.Offset > 0 || x.Repeat.Limit > 0)))
        {
            ret[node.Repeat.Field + PaginationConstants.OffsetSuffix] = node.Repeat.Offset.ToString();
            ret[node.Repeat.Field + PaginationConstants.LimitSuffix] = node.Repeat.Limit.ToString();
        }
        return ret;
    }

    private async Task LoadRelatedData(string name, Record data, QueryArgs args, RepeatNode[] nodes, CancellationToken token)
    {
        foreach (var repeatNode in nodes.Where(x => !string.IsNullOrWhiteSpace(x.Repeat.Query)))
        {
            var pagination = new Pagination(repeatNode.Repeat.Offset, repeatNode.Repeat.Limit);
            var qs = QueryHelpers.ParseQuery(repeatNode.Repeat.QueryString);
            var result = await querySvc.List(repeatNode.Repeat.Query, new Span(), pagination, RenderUtil.MergeDict(args,qs), token);
            data[repeatNode.Repeat.Field] = result;
        }
    }
    private static void TagPagination(PageContext ctx, Record data)
    {
        foreach (var node in ctx.Nodes.Where(x=>x.Repeat.Offset > 0 || x.Repeat.Limit > 0))
        {
            if (data.TryGetValue(node.Repeat.Field, out var val) && val is Record[] records)
            {
                TagPagination(data, records, node.ToPartialToken(ctx.Page.Name));
            }
        }
    }
    
    private static void TagPagination(Record data, Record[] items, PartialToken token)
    {
        if (SpanHelper.HasPrevious(items))
        {
            data[RenderUtil.FirstAttributeTag(token.Repeat.Field) ] = (token with { First = SpanHelper.FirstCursor(items), Last = ""}).ToString();
        }

        if (SpanHelper.HasNext(items))
        {
            data[RenderUtil.LastAttributeTag(token.Repeat.Field)] = (token with { Last = SpanHelper.LastCursor(items),First = ""}).ToString();
        }
    }
    record Context(Page Page, HtmlDocument Doc)
    {
        public PartialContext ToPartialContext(string nodeId)
        {
            return new PartialContext(Page, Doc, Doc.GetElementbyId(nodeId));
        }
    
        public PageContext ToPageContext()
        {
            return new PageContext(Page, Doc, CheckResult(Doc.GetRepeatingNodes()));
        }
    }

    record PageContext(Page Page, HtmlDocument HtmlDocument, RepeatNode[] Nodes);
    
    record PartialContext(Page Page, HtmlDocument HtmlDocument, HtmlNode Node);
    
    private async Task<Context> GetContext(string name, bool matchPrefix, CancellationToken token)
    {
        var res = matchPrefix
            ? await schemaSvc.GetByNamePrefixDefault(name, SchemaType.Page, token)
            : await schemaSvc.GetByNameDefault(name, SchemaType.Page, token);
        
        var schema = NotNull(res).ValOrThrow($"Can not find page [{name}]");
        var page = NotNull(schema.Settings.Page).ValOrThrow("Invalid page payload");
        var doc = new HtmlDocument();
        doc.LoadHtml(page.Html);
        return new Context(page, doc);
    }
}

