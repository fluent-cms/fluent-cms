using System.Text.Json;
using FluentCMS.Models;
using FluentCMS.Models.Queries;

namespace Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestDeserialize()
    {
        var str =
            @"{""Name"":""posts"",""Settings"":{""Entity"":{""TableName"":""posts"",""DefaultPageSize"":0}},""Type"":""entity""}";
        var s = JsonSerializer.Deserialize<SchemaDto>(str);
        Assert.Pass();

    }

    [Test]
    public void TestSerialize()
    {
        var entity = new Entity()
        {
            TableName = "test",Title = "kkk"
            
        };
        var schema = new SchemaDto()
        {
            Name = "def", Settings = new Settings()
            {
                Entity = entity
                
            }
        };

        var str = JsonSerializer.Serialize(schema, new JsonSerializerOptions());
        Console.WriteLine(str);
        Assert.Pass();
    }
}