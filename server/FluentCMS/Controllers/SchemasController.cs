using FluentCMS.Models;
using FluentCMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class SchemasController(ISchemaService schemaService):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Schema>>> GetAll([FromQuery]string? type )
    {
        return Ok(await schemaService.GetAll(type??""));
    }
    
    [HttpPost]
    public async Task<ActionResult<Schema>> Post([FromBody] Schema dto)
    {
        var item = await schemaService.Save(dto);
        return  Ok(item);
    }
    [HttpPost("define")]
    public async Task<ActionResult<Schema>> SaveTableDefine( [FromBody] Schema dto)
    {
        var item = await schemaService.SaveTableDefine(dto);
        return Ok(item);
    }

    [HttpPost("simple_entity_define")]
    public async Task<ActionResult<Schema>> EnsureSimpleEntity(
        [FromQuery] string entity,
        [FromQuery] string field,
        [FromQuery] string? lookup,
        [FromQuery] string? crosstable) =>
        Ok(await schemaService.AddOrSaveSimpleEntity(entity, field, lookup, crosstable));
    
    [HttpGet("{name}/define")]
    public async Task<ActionResult<Schema>> GetTableDefine(string name)
    {
        var item = await schemaService.GetTableDefine(name);
        return Ok(item);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<Schema>> GetOne(string name, [FromQuery] bool? extend)
    {
        return Ok(await schemaService.GetByIdOrName(name, extend?? true));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await schemaService.Delete(id);
        return NoContent();
    }

   
}