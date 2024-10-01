using FluentResults;
using SkiaSharp; // Ensure SkiaSharp NuGet package is installed

namespace FluentCMS.Utils.LocalFileStore;

public class LocalFileStore(string pathPrefix, int maxImageWith, int quality)
{
    public async Task<Result<string[]>> Save(IFormFile[] files)
    {
        var dir = GetDirectoryName();
        Directory.CreateDirectory(Path.Combine(pathPrefix, dir));
        List<string> ret = new();

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                return Result.Fail($"Invalid file length {file.FileName}");
            }

            var fileName = Path.Combine(dir, GetFileName(file.FileName));
            await using var saveStream = File.Create(Path.Combine(pathPrefix, fileName));

            if (IsImage(file))
            {
                await using var inStream = file.OpenReadStream();
                CompressImage(inStream, saveStream );
            }
            else
            {
                await file.CopyToAsync(saveStream);
            }
            ret.Add("/" + fileName);
        }

        return ret.ToArray();
    }

    private bool IsImage(IFormFile file)
    {
        string[] validExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        return validExtensions.Contains(ext);
    }

    private void CompressImage(Stream inStream, Stream outStream)
    {
        using var originalBitmap = SKBitmap.Decode(inStream);
        if (originalBitmap.Width > maxImageWith)
        {
            var scaleFactor = (float)maxImageWith / originalBitmap.Width;
            var newHeight = (int)(originalBitmap.Height * scaleFactor);

            var resizedImage = originalBitmap.Resize(new SKImageInfo(maxImageWith, newHeight), SKFilterQuality.Medium);
            resizedImage?.Encode(outStream, SKEncodedImageFormat.Jpeg, quality);
        }
        inStream.CopyToAsync(outStream);
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
