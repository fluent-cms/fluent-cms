using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Controllers;
[ApiController]
[Route("api/[controller]")]
public class QueriesController(IQueryService queryService) : ControllerBase
{
    [HttpGet("{name}")]
    public async Task<ActionResult> GetList(string name, [FromQuery] Span span, [FromQuery] Pagination pagination,CancellationToken token)
    {
        var dict = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var res = await queryService.List(name, span,pagination, dict,token);
        return Ok( res);
   
    }
    [HttpGet("{name}/one")]
    public async Task<ActionResult> GetOne(string name, CancellationToken token)
    {
        var args = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.One(name, args,token));
    }

    [HttpGet("{name}/partial/{attr}")]
    public async Task<ActionResult> GetPartial(string name, string attr, [FromQuery] Span span, [FromQuery] int limit, CancellationToken token)
    {
        var args = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.Partial(name, attr, span, limit,args, token));
    }

    [HttpGet("{name}/many")]
    public async Task<ActionResult> GetMany(string name, CancellationToken token)
    {
        var args = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.Many(name, args,token));
    }
}