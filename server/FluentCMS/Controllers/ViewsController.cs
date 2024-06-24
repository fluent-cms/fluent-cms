using FluentCMS.Models.Queries;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ViewsController(IViewService viewService): ControllerBase
{
    [HttpGet("{viewName}/{query}")]
    public async Task<ActionResult<IEnumerable<object>>> Get(string viewName, Pagination? pagination)
    {
        var items = await viewService.List(viewName, pagination);
        return items is null ? NotFound() : Ok(items);
    }
    
}