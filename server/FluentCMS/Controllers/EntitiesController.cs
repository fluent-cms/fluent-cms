using System.Text.Json;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EntitiesController(IEntityService entityService) : ControllerBase
{
    [HttpGet("{entityName}")]
    public async Task<ActionResult<ListResult>> List(string entityName, [FromQuery] Pagination? pagination)
        => Ok(await entityService.List(entityName, pagination,
            QueryHelpers.ParseQuery(HttpContext.Request.QueryString.Value)));

    [HttpGet("{entityName}/{id}")]
    public async Task<ActionResult<object>> One(string entityName, string id) =>
        Ok(await entityService.One(entityName, id));

    [HttpPost("{entityName}/insert")]
    public async Task<ActionResult<int>> Insert(string entityName, [FromBody] JsonElement item) =>
        Ok(await entityService.Insert(entityName, item));

    [HttpPost("{entityName}/update")]
    public async Task<ActionResult<int>> Update(string entityName, [FromBody] JsonElement item) =>
        Ok(await entityService.Update(entityName, item));

    [HttpPost("{entityName}/delete")]
    public async Task<ActionResult<int>> Delete(string entityName, [FromBody] JsonElement item) =>
        Ok(await entityService.Delete(entityName, item));

    [HttpPost("{entityName}/{id}/{attributeName}/delete")]
    public async Task<ActionResult<int>> CrosstableDelete(string entityName, string id, string attributeName,
        [FromBody] JsonElement[] items) =>
        Ok(await entityService.CrosstableDelete(entityName, id, attributeName, items));


    [HttpPost("{entityName}/{id}/{attributeName}/save")]
    public async Task<ActionResult<int>> CrosstableSave(string entityName, string id, string attributeName,
        [FromBody] JsonElement[] items) => Ok(await entityService.CrosstableSave(entityName, id, attributeName, items));

    [HttpGet("{entityName}/{id}/{attributeName}")]
    public async Task<ActionResult<object>> CrosstableList(string entityName, string id, string attributeName,
        [FromQuery] bool exclude) => Ok(await entityService.CrosstableList(entityName, id, attributeName, exclude));
}