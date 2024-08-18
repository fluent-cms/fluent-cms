using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Controllers;
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class ViewsController(IViewService viewService) : ControllerBase
{
    [HttpGet("{viewName}")]
    public async Task<ActionResult<ListResult>> Get(string viewName, CancellationToken cancellationToken,
        [FromQuery] Cursor cursor)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var res = await viewService.List(viewName, cursor, queryDictionary, cancellationToken);
        return Ok( res);
   
    }
    [HttpGet("{viewName}/one")]
    public async Task<ActionResult<Record>> GetOne(string viewName, CancellationToken cancellationToken)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await viewService.One(viewName,  queryDictionary,cancellationToken));
    }

    [HttpGet("{viewName}/many")]
    public async Task<ActionResult<IDictionary<string, object>[]>> GetMany(string viewName,
        CancellationToken cancellationToken)
    {
        var queryDictionary = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        return Ok(await viewService.Many(viewName, queryDictionary,cancellationToken));
    }
}