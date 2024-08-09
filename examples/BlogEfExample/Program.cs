using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostEfExample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("Postgres") 
                      ?? builder.Configuration.GetConnectionString("Postgres")));

var app = builder.Build();

app.MapGet("/posts_join",async (BloggingContext context,[FromQuery] string? last) =>
{
    DateTime? ts = null;
    if (!string.IsNullOrWhiteSpace(last))
    {
        ts = JsonSerializer.Deserialize<Cursor>(Decode(last))?.Ts;
    }
    var posts = await context.Posts
       .Where(p => !p.Deleted && (ts == null || p.PublishedAt.ToLocalTime() < ts))
       .OrderByDescending(p => p.PublishedAt)
       .Take(10)
       .Select(p => new
       {
           p.Id,
           p.Title,
           p.PublishedAt,
           p.Slug,
           p.CategoryId,
           p.ThumbnailImage,
           CategoryIdData = context.Categories
               .Where(c => c.Id == p.CategoryId && !c.Deleted)
               .Select(c => new
               {
                   c.Id,
                   c.Name,
                   c.ParentCategoryId,
                   c.FeaturedImage,
                   c.ThumbnailImage,
                   c.CreatedAt,
                   c.UpdatedAt,
                   c.Slug
               })
               .FirstOrDefault(),
           Authors = context.PostAuthors
               .Where(ap => ap.PostId == p.Id && !ap.Deleted)
               .Join(context.Authors,
                     ap => ap.AuthorId,
                     a => a.Id,
                     (ap, a) => new
                     {
                         a.Id,
                         a.Name,
                         a.Slug,
                         a.ThumbnailImage,
                         a.FeaturedImage,
                         a.CreatedAt,
                         a.UpdatedAt
                     })
               .ToList()
       })
       .ToListAsync();
    var lastItem = new Cursor{Ts = posts.Last().PublishedAt}; 
    return new { items = posts, last = Encode( JsonSerializer.Serialize(lastItem))};

});

app.MapGet("/posts", async (BloggingContext context,[FromQuery] string? last) =>
{
    DateTime? ts = null;
    if (!string.IsNullOrWhiteSpace(last))
    {
        ts = JsonSerializer.Deserialize<Cursor>(Decode(last))?.Ts;
    }

    var posts = await context.Posts
        .Where(p => !p.Deleted && (ts == null || p.PublishedAt.ToLocalTime() < ts))
        .OrderByDescending(p => p.PublishedAt)
        .Take(10)
        .Select(p => new
        {
            p.Id,
            p.Title,
            p.PublishedAt,
            p.Slug,
            p.CategoryId,
            p.ThumbnailImage
        })
        .ToListAsync();
    var categoryIds = posts.Select(p => p.CategoryId).Distinct().ToList();

    var categories = await context.Categories
        .Where(c => categoryIds.Contains(c.Id))
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.ParentCategoryId,
            c.FeaturedImage,
            c.ThumbnailImage,
            c.CreatedAt,
            c.UpdatedAt,
            c.Slug
        })
        .ToListAsync();

    var postIds = posts.Select(p => p.Id).ToList();

    var authors = await (from pa in context.PostAuthors
        join a in context.Authors on pa.AuthorId equals a.Id
        where postIds.Contains(pa.PostId) && !pa.Deleted && !a.Deleted
        select new
        {
            pa.PostId,
            Author = new
            {
                a.Id,
                a.Name,
                a.Slug,
                a.ThumbnailImage,
                a.FeaturedImage,
                a.CreatedAt,
                a.UpdatedAt
            }
        }).ToListAsync();


    var result =  posts.Select(p => new
    {
        p.Id,
        p.Title,
        p.PublishedAt,
        p.Slug,
        p.CategoryId,
        p.ThumbnailImage,
        CategoryIdData = categories.FirstOrDefault(c => c.Id == p.CategoryId),
        Authors = authors.Where(a => a.PostId == p.Id).Select(a => a.Author).ToList()
    }).ToList();


    var lastItem = new Cursor{Ts = result.Last().PublishedAt}; 
    return new { items = result, last = Encode( JsonSerializer.Serialize(lastItem))};
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