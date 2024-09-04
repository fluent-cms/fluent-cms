using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Cms.Controllers;

[ApiController]
[Route("[controller]")]
public class PagesController(IPageService pageService):ControllerBase
{
    [HttpGet("{pageName}")]
    public async Task<ActionResult> Get(string pageName, [FromQuery] Cursor cursor, CancellationToken cancellationToken)
    {
        var htmlContent = await pageService.Get(pageName, cursor, cancellationToken);
        return Content(htmlContent, "text/html");
    } 
}