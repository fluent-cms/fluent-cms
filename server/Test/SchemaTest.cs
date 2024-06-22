using System.Text.Json;
using FluentCMS.Models;
using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;
using Npgsql;
using SqlKata;

namespace Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestObjectEq()
    {
        object a = 1;
        object b = 1;
        if (a == b)
        {
            Console.WriteLine("eq");
        }
            Console.WriteLine("not eq");
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
    public async Task TestPgDaoGet()
    {
        string connectionString = "Host=localhost;Username=postgres;Password=mysecretpassword;Database=fluent-cms";
        var dao = new PgDao(connectionString, true);
        var items = await dao.Many(new Query("posts").Select("id","title"));
        Assert.Pass();
    }
    [Test]
    public void TestInt()
    {
        string connectionString = "Host=localhost;Username=postgres;Password=mysecretpassword;Database=fluent-cms";
        
        
        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();


            using (var cmd = new NpgsqlCommand("SELECT * FROM posts WHERE id = '1'", conn))
            {

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Process each row
                        // Example: Console.WriteLine(reader["column_name"]);
                        Console.WriteLine($"Id: {reader["id"]}, Title: {reader["title"]}, Content: {reader["content"]}");
                    }
                }
            }
        }
        
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