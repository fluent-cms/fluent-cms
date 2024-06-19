using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EntitiesController(IEntityService entityService):ControllerBase
{
    [HttpGet("{entityName}")]
     public async Task<ActionResult<IEnumerable<object>>> Get(string entityName)
     {
         var items = await entityService.GetAll(entityName);
         if (items is null)
         {
             return NotFound();
         }
         return Ok(items);
     }   
}