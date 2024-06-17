using FluentCMSApi.models;
using FluentCMSApi.Services.cs;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMSApi.Controllers;
[ApiController]
[Route("api/[controller]")]
public class SchemaController(ISchemaService schemaService):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Schema>>> Get()
    {
        return Ok(await schemaService.GetAll());
    }
    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Schema>> Post([FromBody] Schema item)
    {
        await schemaService.Add(item);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }
    // PUT: api/products/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] Schema item)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        await schemaService.Update(item);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await schemaService.Delete(id);
        return NoContent();
    }

    
}