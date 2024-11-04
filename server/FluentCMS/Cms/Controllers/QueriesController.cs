using System.Collections.Immutable;
using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Controllers;
[ApiController]
[Route("api/[controller]")]
public class QueriesController(IQueryService queryService) : ControllerBase
{
    [HttpGet("{queryName}")]
    public async Task<ActionResult> GetList(string queryName,
        [FromQuery] Cursor cursor, [FromQuery] Pagination pagination,CancellationToken cancellationToken)
    {
        var dict = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var res = await queryService.List(queryName, cursor,pagination, dict,dict,cancellationToken);
        return Ok( res);
   
    }
    [HttpGet("{queryName}/one")]
    public async Task<ActionResult> GetOne(string pageName, CancellationToken cancellationToken)
    {
        var dict = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.One(pageName, dict,dict,cancellationToken));
    }

    [HttpGet("{queryName}/partial/{attrPath}")]
    public async Task<ActionResult> GetPartial(string pageName, string attrPath, 
        [FromQuery] Cursor cursor, [FromQuery] int limit, CancellationToken cancellationToken)
    {
        return Ok(await queryService.Partial(pageName, attrPath, cursor, limit, cancellationToken));
    }

    [HttpGet("{queryName}/many")]
    public async Task<ActionResult> GetMany(string queryName,
        CancellationToken cancellationToken)
    {
        var dict = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.Many(queryName, dict,dict,cancellationToken));
    }
}