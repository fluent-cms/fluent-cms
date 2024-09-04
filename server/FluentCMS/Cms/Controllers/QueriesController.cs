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
    [HttpGet("{pageName}")]
    public async Task<ActionResult<ListResult>> Get(string pageName, CancellationToken cancellationToken,
        [FromQuery] Cursor cursor)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var res = await queryService.List(pageName, cursor, queryDictionary,cancellationToken);
        return Ok( res);
   
    }
    [HttpGet("{pageName}/one")]
    public async Task<ActionResult<Record>> GetOne(string pageName, CancellationToken cancellationToken)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.One(pageName,  queryDictionary,cancellationToken));
    }

    [HttpGet("{pageName}/many")]
    public async Task<ActionResult<IDictionary<string, object>[]>> GetMany(string pageName, [FromQuery] Pagination?pagination,
        CancellationToken cancellationToken)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await queryService.Many(pageName,pagination, queryDictionary,cancellationToken));
    }
}