using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostEfExample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("Postgres") ?? builder.Configuration.GetConnectionString("Postgres")));

var app = builder.Build();

app.MapGet("/posts", async (BloggingContext context,[FromQuery] string? last) =>
{
    DateTime? ts = null;
    if (!string.IsNullOrWhiteSpace(last))
    {
        ts = JsonSerializer.Deserialize<Cursor>(Decode(last))?.Ts;
    }

    var query = context.Posts
        .Include(p => p.Category)
        .Include(p => p.PostAuthors)
        .ThenInclude(pa => pa.Author)
        .Where(p => 
            !p.Deleted 
            && (ts == null || p.PublishedAt.ToLocalTime() < ts ) 
            &&  !p.Category.Deleted 
            && p.PostAuthors.All(pa => !pa.Author.Deleted))
        .OrderByDescending(p=>p.PublishedAt)
        .Select(p => new
        {
            p.Id,
            p.Title,
            p.PublishedAt,
            p.Slug,
            p.CategoryId,
            p.ThumbnailImage,
            CategoryIdData = new
            {
                p.Category.Id,
                p.Category.Name,
                p.Category.ParentCategoryId,
                p.Category.FeaturedImage,
                p.Category.ThumbnailImage,
                p.Category.CreatedAt,
                p.Category.UpdatedAt,
                p.Category.Slug
            },
            Authors = p.PostAuthors.Select(pa => new
            {
                pa.Author.Id,
                pa.Author.Name,
                pa.Author.Slug,
                pa.Author.ThumbnailImage,
                pa.Author.FeaturedImage,
                pa.Author.CreatedAt,
                pa.Author.UpdatedAt,
                PostId = p.Id
            })

        }).Take(10);
    var posts = await query.ToListAsync();
    var lastItem = new Cursor{Ts = posts.Last().PublishedAt}; 
    return new { items = posts, last = Encode( JsonSerializer.Serialize(lastItem))};
});

app.Run();

string Decode(string input)
{
    input = input.Replace('-', '+').Replace('_', '/');
    switch (input.Length % 4)
    {
        case 2: input += "=="; break;
        case 3: input += "="; break;
    }
    var bs = Convert.FromBase64String(input);
    return Encoding.UTF8.GetString(bs);
}

string Encode(string input)
{
    var output = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    output = output.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    return output;
}