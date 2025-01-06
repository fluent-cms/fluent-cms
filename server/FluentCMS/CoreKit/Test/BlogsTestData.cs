using FluentCMS.CoreKit.ApiClient;
using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.ResultExt;
using QueryBuilder_Attribute = FluentCMS.Core.Descriptors.Attribute;

namespace FluentCMS.CoreKit.Test;

public record EntityData(string EntityName, string TableName, Record[] Records);
public record JunctionData(string EntityName,  string Attribute,string JunctionTableName, string SourceField, string TargetField, int SourceId, int[] TargetIds);

/// a set of blog entities to test query
public static class BlogsTestData
{
    public static async Task EnsureBlogEntities(SchemaApiClient client)
    {
        await EnsureBlogEntities(x => client.EnsureEntity(x).Ok());
    }
    public static async Task EnsureBlogEntities(Func<Entity, Task> action)
    {
        foreach (var entity in Entities)
        {
            await  action(entity);
        }
    }

    public static async Task PopulateData(EntityApiClient client)
    {
        await PopulateData(1, 100, async data =>
        {
            foreach (var dataRecord in data.Records)
            {
                await client.Insert(data.EntityName, dataRecord).Ok();
            }
        }, async data =>
        {
            //var items = data.TargetIds.Select(x => new { id = x });
            foreach (var dataTargetId in data.TargetIds)
            {
                await client.JunctionAdd(data.EntityName, data.Attribute, data.SourceId, dataTargetId).Ok();
            }
        });
    }

    public static async Task PopulateData(int startId, int count, Func<EntityData,Task> insertEntity, Func<JunctionData,Task> insertJunction)
    {
        var tags = new List<Record>();
        var authors = new List<Record>();
        var categories = new List<Record>();
        var posts = new List<Record>();
        var tagsIds = new List<int>();
        var authorsIds = new List<int>();
        var attachments = new List<Record>();
        
        for (var i = startId; i < startId + count; i++)
        {
            tagsIds.Add(i);
            authorsIds.Add(i);
            
            tags.Add(GetObject(["name", "description", "image"], i));
            authors.Add(GetObject(["name", "description", "image"], i));
            categories.Add(GetObject(["name", "description", "image"], i));
            
            var post = GetObject(["title", "abstract","body","image"], i);
            post["category"] = i;
            posts.Add(post);
            
            var attachment = GetObject(["name","description", "image"], i);
            attachment["post"] = startId;
            attachments.Add(attachment);
        }

        await insertEntity(new EntityData("tag", "tags", tags.ToArray()));
        await insertEntity(new EntityData("author", "authors", authors.ToArray()));
        await insertEntity(new EntityData("category", "categories", categories.ToArray()));
        await insertEntity(new EntityData("post", "posts", posts.ToArray()));
        await insertEntity(new EntityData("attachment", "attachments", attachments.ToArray()));

        await insertJunction(new JunctionData("post", "tags", 
            "post_tag", "post_id", "tag_id", startId, tagsIds.ToArray()));
        await insertJunction(new JunctionData("post","authors",
            "author_post","post_id","author_id",startId,authorsIds.ToArray()));
    }
    private static Dictionary<string,object> GetObject(string[] fields, int i)
    {
        var returnValue = new Dictionary<string, object>();
        foreach (var field in fields)
        {
            returnValue.Add(field, $"{field}-{i}");
        }
        return returnValue; 

    }

    private static readonly Entity[] Entities =
    [
        new(
            Attributes:
            [
                new QueryBuilder_Attribute(Field: "name", Header: "Name"),
                new QueryBuilder_Attribute(Field: "description", Header: "Description"),
                new QueryBuilder_Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
            ],
            DefaultPageSize: 50,
            TitleAttribute: "name",
            TableName: "tags",
            Title: "Tag",
            Name: "tag"
        ),
        new(
            Attributes:
            [
                new QueryBuilder_Attribute(Field: "name", Header: "Name"),
                new QueryBuilder_Attribute(Field: "description", Header: "Description"),
                new QueryBuilder_Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                new QueryBuilder_Attribute(Field: "post", Header: "Post", DataType:DataType.Int, DisplayType: DisplayType.Number),
            ],
            DefaultPageSize: 50,
            TitleAttribute: "name",
            TableName: "attachments",
            Title: "Attachment",
            Name: "attachment"
        ),
        new(
            Attributes:
            [
                new QueryBuilder_Attribute(Field: "name", Header: "Name"),
                new QueryBuilder_Attribute(Field: "description", Header: "Description"),
                new QueryBuilder_Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
            ],
            TitleAttribute: "name",
            TableName: "authors",
            DefaultPageSize: 50,
            Title: "Author",
            Name: "author"
        ),
        new (
            Attributes:
            [
                new QueryBuilder_Attribute(Field: "name", Header: "Name"),
                new QueryBuilder_Attribute(Field: "description", Header: "Description"),
                new QueryBuilder_Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
            ],
            TitleAttribute: "name",
            TableName: "categories",
            Title: "Category",
            DefaultPageSize: 50,
            Name: "category"
        ),
        new (
            Attributes:
            [
                new QueryBuilder_Attribute(Field: "title", Header: "Title"),
                new QueryBuilder_Attribute(Field: "abstract", Header: "Abstract"),
                new QueryBuilder_Attribute(Field: "body", Header: "Body"),
                new QueryBuilder_Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                
                new QueryBuilder_Attribute(Field: "attachments", Header: "Attachments", DataType: DataType.Collection,
                    DisplayType: DisplayType.EditTable,
                    Options: "attachment.post"),
                
                new QueryBuilder_Attribute(Field: "tags", Header: "Tag", DataType: DataType.Junction,
                    DisplayType: DisplayType.Picklist,
                    Options: "tag"),
                new QueryBuilder_Attribute(Field: "authors", Header: "Author", DataType: DataType.Junction,
                    DisplayType: DisplayType.Picklist,
                    Options: "author"),
                new QueryBuilder_Attribute(Field: "category", Header: "Category", DataType: DataType.Lookup,
                    DisplayType: DisplayType.Lookup, Options: "category"),
            ],
            TitleAttribute: "title",
            TableName: "posts",
            Title: "Post",
            DefaultPageSize: 50,
            Name: "post"
        )
    ];

}