using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using Moq;

namespace Utils.Tests.HookFactoryTests;
using Record = IDictionary<string,object>;

public class People
{
    public const string EntityName = "people";
    public const string NameFieldName = "Name";
    public const string NameValue = "Alice";
    public int id { get; set; }
    public string Name { get; set; } = "";
}

public class PeopleService
{
    public void ModifyRecord(Record dictionary, string s)
    {
        dictionary[People.NameFieldName] += s;
    }

    public void ModifyPeople(People people, string s)
    {
        people.Name += s;
    }
    public async Task<People> ModifyPeopleAsyncReturn(People people)
    {
        await Task.Delay(10);
        people.Name += "ModifyPeopleAsyncReturn";
        return new People
        {
            id = people.id,
            Name = people.Name
        };
    }
    public async Task ModifyPeopleAsync(People people)
    {
        await Task.Delay(10);
        people.Name += "ModifyPeopleAsync";
    }
}

public class HookFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HookFactory _hookFactory;
    
    public HookFactoryTests()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(PeopleService)))
            .Returns(new PeopleService());

        _serviceProvider = mockServiceProvider.Object;
        _hookFactory = new HookFactory();
    }

    [Fact]
    public async Task TestExecuteAfterQuery()
    {
        var occasion = Occasion.AfterQueryMany;
        _hookFactory.AddHook(People.EntityName, occasion, Next.Exit, (ListResult res) =>
        {
            res.TotalRecords = 100;
        });

        var res = new ListResult
        {
            TotalRecords = 10,
        };
        await _hookFactory.ExecuteAfterQuery(_serviceProvider, occasion, People.EntityName, res);
        Assert.Equal(100, res.TotalRecords);
    }

    [Fact]
    public async Task TestExecuteBeforeQueryReplace()
    {
        var occasion = Occasion.AfterQueryMany;
        _hookFactory.AddHook(People.EntityName,occasion,Next.Exit, () =>new People
        {
            id = 3,Name = People.NameValue
        } );
        
        var (filters, sorts, pagination) = (new Filters(), new Sorts(), new Pagination());
        var (obj,next) = await _hookFactory.
            ExecuteBeforeQuery(_serviceProvider, occasion, People.EntityName, filters, sorts, pagination);
        Assert.Equal(Next.Exit,next);
        Assert.Equal(People.NameValue, ((People)obj!).Name);
    }

    [Fact]
    public async Task TestExecuteBeforeQuery()
    {
        var occasion = Occasion.AfterQueryMany;
        _hookFactory.AddHook(People.EntityName,occasion,Next.Continue, (Filters filters1, Sorts sorts1, Pagination pagination1) =>
        {
            filters1.Add(new Filter());
            sorts1.Add(new Sort());
            pagination1.Offset = 100;
        });
        
        var (filters, sorts, pagination) = (new Filters(), new Sorts(), new Pagination());
        await _hookFactory.ExecuteBeforeQuery(_serviceProvider, occasion,People.EntityName, filters, sorts, pagination);
        Assert.Single(filters);
        Assert.Single(sorts);
        Assert.Equal(100,pagination.Offset);

    }
    [Fact]
    public async Task TestAsyncReturnStringToObject()
    {
        var occasion = Occasion.BeforeQueryOne;
        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            (string id) => new People { id = Int32.Parse(id), Name = People.NameValue });

        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            async (PeopleService service, People people) =>await service.ModifyPeopleAsyncReturn(people));

        var (res, _) = await _hookFactory.ExecuteStringToObject(_serviceProvider, occasion, "people", "1");
        var people = (People)res;
        Assert.Equal(1, people.id);
        Assert.Equal(People.NameValue + "ModifyPeopleAsyncReturn", people.Name);
    }

    [Fact]
    public async Task TestAsyncStringToObject()
    {
        var occasion = Occasion.BeforeQueryOne;

        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            (string id) => new People { id = Int32.Parse(id), Name = People.NameValue });

        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            async (PeopleService service, People people) => await service.ModifyPeopleAsync(people));

        var (res, _) = await _hookFactory.ExecuteStringToObject(_serviceProvider, occasion, "people", "1");
        var people = (People)res;
        Assert.Equal(1, people.id);
        Assert.Equal(People.NameValue + "ModifyPeopleAsync", people.Name);
    }

    [Fact]
    public async Task TestStringToObject()
    {
        var occasion = Occasion.BeforeQueryOne;
        
        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            (string id) => new People{id = Int32.Parse(id), Name = People.NameValue});

        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people,"ModifyPeople"));
        
        var (res,_ )= await _hookFactory.ExecuteStringToObject(_serviceProvider, occasion, "people", "1");
        var people = (People)res;
        Assert.Equal(1, people.id);
        Assert.Equal(People.NameValue + "ModifyPeople", people.Name);
    }
    
    [Fact]
    public async Task TestRecordToObject()
    {
        var occasion = Occasion.AfterQueryOne;
        
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            [People.NameFieldName] = People.NameValue
        };
        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            (PeopleService service, Record record) => service.ModifyRecord(record,"ModifyRecord"));

        _hookFactory.AddHook(People.EntityName, occasion, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people,"ModifyPeople"));
        var (res,_ )= await _hookFactory.ExecuteRecordToObject(_serviceProvider, occasion, People.EntityName, records);
        var people = (People)res;
        Assert.Equal(People.NameValue + "ModifyRecord" + "ModifyPeople", people.Name);
    }

    [Fact]
    public async Task TestRecordToRecordExit()
    {
        var name = "Alice";
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            [People.NameFieldName] = name
        };

        _hookFactory.AddHook(People.EntityName, Occasion.BeforeInsert, Next.Exit,
            (PeopleService service, Record record) => service.ModifyRecord(record,"ModifyRecord"));
        
        _hookFactory.AddHook(People.EntityName, Occasion.BeforeInsert, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people,"ModifyPeople"));
        
        var (res,_ )= await _hookFactory.ExecuteRecordToRecord(_serviceProvider, Occasion.BeforeInsert, "people", records);
        Assert.Equal(name + "ModifyRecord" , res[People.NameFieldName]);
    }
    
    [Fact]
    public async Task TestRecordToRecord()
    {
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            [People.NameFieldName] = People.NameValue
        };

        _hookFactory.AddHook(People.EntityName, Occasion.BeforeInsert, Next.Continue,
            (PeopleService service, Record record) => service.ModifyRecord(record,"ModifyRecord"));
        
        _hookFactory.AddHook(People.EntityName, Occasion.BeforeInsert, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people,"ModifyPeople"));
        
        var (res,_ )= await _hookFactory.ExecuteRecordToRecord(_serviceProvider, Occasion.BeforeInsert, "people", records);
        Assert.Equal(People.NameValue + "ModifyRecord" + "ModifyPeople", res[People.NameFieldName]);
    }
}