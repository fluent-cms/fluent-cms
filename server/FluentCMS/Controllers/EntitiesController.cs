using System.Text.Json;
using FluentCMS.Models.Queries;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EntitiesController(IEntityService entityService) : ControllerBase
{
    [HttpGet("{entityName}")]
    public async Task<ActionResult<IEnumerable<object>>> Get(string entityName,[FromQuery] Pagination? pagination)
    {
        var qs = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value);
        var sorts = new Sorts(qs);
        var filters = new Filters(qs); 
        var items = await entityService.List(entityName, pagination, sorts, filters);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("{entityName}/{id}")]
    public async Task<ActionResult<object>> Get(string entityName, string id)
    {
        var items = await entityService.One(entityName, id);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPost("{entityName}/insert")]
    public async Task<ActionResult<int>> Insert(string entityName, [FromBody] JsonElement item)
    {
        var id = await entityService.Insert(entityName, item);
        return id is null ? NotFound() : Ok(id);
    }

    [HttpPost("{entityName}/update")]
    public async Task<ActionResult<int>> Update(string entityName, [FromBody] JsonElement item)
    {
        var id = await entityService.Update(entityName, item);
        return id is null ? NotFound() : Ok(id);
    }

    [HttpPost("{entityName}/delete")]
    public async Task<ActionResult<int>> Delete(string entityName, [FromBody] JsonElement item)
    {
        var id = await entityService.Delete(entityName, item);
        return id is null ? NotFound() : Ok(id);
    }
    
    [HttpPost("{entityName}/{id}/{attributeName}/delete")]
    public async Task<ActionResult<int>> CrosstableDelete(string entityName, string id, string attributeName, [FromBody] JsonElement[] items)
    {
        var ret = await entityService.CrosstableDelete(entityName, id, attributeName, items);
        return ret is null ? NotFound() : Ok(ret);
    }
    

    [HttpPost("{entityName}/{id}/{attributeName}/save")]
    public async Task<ActionResult<int>> CrosstableSave(string entityName, string id, string attributeName, [FromBody] JsonElement[] items)
    {
        var ret = await entityService.CrosstableSave(entityName, id, attributeName, items);
        return ret is null ? NotFound() : Ok(ret);
    }

    [HttpGet("{entityName}/{id}/{attributeName}")]
    public async Task<ActionResult<object>> CrosstableList(string entityName, string id, string attributeName, [FromQuery] bool exclude)
    {
        var items = await entityService.CrosstableList(entityName, id, attributeName, exclude);
        return items is null? NotFound() : Ok(items);
    }

}