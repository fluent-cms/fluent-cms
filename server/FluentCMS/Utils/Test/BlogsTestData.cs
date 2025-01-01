using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.Test;

/// a set of blog entities to test query
public static class BlogsTestData
{
    public static async Task EnsureBlogEntities(SchemaApiClient client)
    {
        foreach (var entity in Entities)
        {
            await client.EnsureEntity(entity).Ok();
        }
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

    public static async Task PopulateData(EntityApiClient client)
    {
        for (var i = 1; i <= 100; i++)
        {
            await client.Insert("tag", GetObject(["name", "description", "image"], i)).Ok();
            await client.Insert("author", GetObject(["name", "description", "image"], i)).Ok();
            await client.Insert("category", GetObject(["name", "description", "image"], i)).Ok();
            
            var post = GetObject(["title", "abstract","body","image"], i);
            post["category"] = new { id = i};
            await client.Insert("post", post).Ok();
        }

        for (var i = 1; i <= 10; i++)
        {
            for (var j = 1; j <= 100; j++)
            {
                await client.JunctionAdd("post", "tags", i , j ).Ok();
                await client.JunctionAdd("post", "authors", i , j).Ok();
                var attachment = GetObject(["name", "description", "image"], j);
                attachment["post"] = i;
                await client.Insert("attachment", attachment).Ok();
            }
        }
    }


    public static readonly Entity[] Entities =
    [
        new(
            Attributes:
            [
                new Attribute(Field: "name", Header: "Name"),
                new Attribute(Field: "description", Header: "Description"),
                new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
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
                new Attribute(Field: "name", Header: "Name"),
                new Attribute(Field: "description", Header: "Description"),
                new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                new Attribute(Field: "post", Header: "Post", DataType:DataType.Int, DisplayType: DisplayType.Number),
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
                new Attribute(Field: "name", Header: "Name"),
                new Attribute(Field: "description", Header: "Description"),
                new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
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
                new Attribute(Field: "name", Header: "Name"),
                new Attribute(Field: "description", Header: "Description"),
                new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
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
                new Attribute(Field: "title", Header: "Title"),
                new Attribute(Field: "abstract", Header: "Abstract"),
                new Attribute(Field: "body", Header: "Body"),
                new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                
                new Attribute(Field: "attachments", Header: "Attachments", DataType: DataType.Collection,
                    DisplayType: DisplayType.EditTable,
                    Options: "attachment.post"),
                
                new Attribute(Field: "tags", Header: "Tag", DataType: DataType.Junction,
                    DisplayType: DisplayType.Picklist,
                    Options: "tag"),
                new Attribute(Field: "authors", Header: "Author", DataType: DataType.Junction,
                    DisplayType: DisplayType.Picklist,
                    Options: "author"),
                new Attribute(Field: "category", Header: "Category", DataType: DataType.Lookup,
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