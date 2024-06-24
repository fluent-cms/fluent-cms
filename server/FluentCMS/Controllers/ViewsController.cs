using FluentCMS.Models.Queries;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ViewsController(IViewService viewService): ControllerBase
{
    [HttpGet("{viewName}")]
    public async Task<ActionResult<IEnumerable<IDictionary<string,object>>>> Get(string viewName,[FromQuery] Pagination? pagination)
    {
        var items = await viewService.List(viewName, pagination);
        return items is null ? NotFound() : Ok(items);
    }
    
}