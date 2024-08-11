using FluentCMS.Utils.Nosql;

namespace FluentCMS.Tests.Utils.Nosql;

public class MongoDaoTests
{
    private readonly MongoDao _mongo = new("mongodb://localhost:27017/test", "test");

    [Fact]
    public async Task InsertCollection()
    {
        Dictionary<string, object>[] authors = [
            new Dictionary<string,object>
            {
                {"id",1},
                {"name","li bai"}
            },
        ];
        Dictionary<string,object>[] items = [
            new Dictionary<string,object>
            {
                {"id", 1},
                {"authors", authors},
            },
        ];
        await _mongo.Insert("test", items);
    }

    [Fact]
    public async Task GetTest()
    {
        await _mongo.List("test");
    }
}