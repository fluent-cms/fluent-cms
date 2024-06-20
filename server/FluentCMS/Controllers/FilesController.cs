using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Controllers;
[ApiController]
[Route("api/[controller]")]
public class FilesController:ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Post(List<IFormFile> files)
    {
        var size = files.Sum(f => f.Length);
        List<string> paths = new();

        foreach (var formFile in files)
        {
            if (formFile.Length == 0)
            {
                continue;
            }

            var filePath = Path.GetTempFileName();
            await using var stream = System.IO.File.Create(filePath);
            await formFile.CopyToAsync(stream);
            paths.Add(filePath);
        }
        return Ok(new { count = files.Count, size, paths });
    } 
}