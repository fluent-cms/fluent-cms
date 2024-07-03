using Microsoft.AspNetCore.Http;

namespace Utils.File;

public class FileUtl(string pathPrefix)
{
    public async Task<string[]> Save(IFormFile[] files)
    {
        var dir = GetDirectoryName();
        Directory.CreateDirectory(Path.Combine(pathPrefix,dir));
        List<string> ret = new();
        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                throw new Exception($"Invalid file length {file.FileName}");
            }

            var fileName = Path.Combine(dir, GetFileName(file.FileName));
            await using var stream = System.IO.File.Create(Path.Combine(pathPrefix, fileName));
            await file.CopyToAsync(stream);
            ret.Add("/" + fileName);
        }

        return ret.ToArray();
    }

    string GetFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var randomString = Guid.NewGuid().ToString("N").Substring(0, 8);
        return randomString + ext;
    }
    
    string GetDirectoryName()
    {
        return DateTime.Now.ToString("yyyy-MM");
    }
    
}