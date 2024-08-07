using FluentCMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Controllers;
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class ViewsController(IViewService viewService) : ControllerBase
{
    [HttpGet("{viewName}")]
    public async Task<ActionResult<ListResult>> Get(string viewName, [FromQuery] Cursor cursor) => Ok(
        await viewService.List(viewName, cursor, QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value)));

    [HttpGet("{viewName}/one")]
    public async Task<ActionResult<Record>> GetOne(string viewName) =>
        Ok(await viewService.One(viewName, QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value)));

    [HttpGet("{viewName}/many")]
    public async Task<ActionResult<IDictionary<string, object>[]>> GetMany(string viewName) =>
        Ok(await viewService.Many(viewName, QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value)));
}