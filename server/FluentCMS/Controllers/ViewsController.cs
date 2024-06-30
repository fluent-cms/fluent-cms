using FluentCMS.Models.Queries;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViewsController(IViewService viewService) : ControllerBase
{
    [HttpGet("{viewName}")]
    public async Task<ActionResult<ListResult>> Get(string viewName,
        [FromQuery] Cursor cursor)
    {
        var items = await viewService.List(viewName, cursor,
            QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value));
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("{viewName}/one")]
    public async Task<ActionResult<Record>> GetOne(string viewName,
        [FromQuery] Pagination? pagination)
    {
        var item = await viewService.One(viewName, QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value));
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{viewName}/many")]
    public async Task<ActionResult<IDictionary<string, object>[]>> GetMany(string viewName,
        [FromQuery] Pagination? pagination)
    {
        var ret = await viewService.Many(viewName, QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value));
        return ret is null ? NotFound() : Ok(ret);
    }
}