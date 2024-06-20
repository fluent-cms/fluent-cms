using FluentCMS.Models;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class SchemasController(ISchemaService schemaService):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchemaDisplayDto>>> Get()
    {
        return Ok(await schemaService.GetAll());
    }
    
    [HttpPost]
    public async Task<ActionResult<Schema>> Post([FromBody] SchemaDto dto)
    {
        var item = await schemaService.Save(dto);
        return item is null ? NotFound(): Ok(item);
    }

    [HttpGet("{id}/define")]
    public async Task<ActionResult<SchemaDisplayDto>> GetTableDefine(int id)
    {
        var columns = await schemaService.GetTableDefine(id);
        return Ok(columns);
    }
    
     [HttpGet("{name}")]
     public async Task<ActionResult<SchemaDisplayDto>> Get(string name)
     {
         return Ok(await schemaService.GetByIdOrName(name));
     }
   

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await schemaService.Delete(id);
        return NoContent();
    }

    
}