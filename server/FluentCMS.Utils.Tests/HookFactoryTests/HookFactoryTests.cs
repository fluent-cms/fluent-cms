using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using Moq;
using Xunit.Abstractions;

namespace Utils.Tests.HookFactoryTests;
using Record = IDictionary<string,object>;

public class People
{
   public int Id { get; set; }
   public string Name { get; set; } = "";
}

public class PeopleService
{
    public void ModifyRecord(Record dictionary)
    {
        dictionary["name"] += "ModifyRecord";
    }

    public void ModifyPeople(People people)
    {
        people.Name += "ModifyPeople";
    }
    public async Task<People> ModifyPeopleAsyncReturn(People people)
    {
        await Task.Delay(10);
        people.Name += "ModifyPeopleAsyncReturn";
        return new People
        {
            Id = people.Id,
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
    private const string EntityName = "people";
    private const string  Name = "Alice";
    
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
        _hookFactory.AddHook(EntityName, occasion, Next.Exit, (ListResult res) =>
        {
            res.TotalRecords = 100;
        });

        var res = new ListResult
        {
            TotalRecords = 10,
        };
        await _hookFactory.ExecuteAfterQuery(_serviceProvider, occasion, EntityName, res);
        Assert.Equal(100, res.TotalRecords);
    }

    [Fact]
    public async Task TestExecuteBeforeQueryReplace()
    {
        var occasion = Occasion.AfterQueryMany;
        _hookFactory.AddHook(EntityName,occasion,Next.Exit, () =>new People
        {
            Id = 3,Name = Name
        } );
        
        var (filters, sorts, pagination) = (new Filters(), new Sorts(), new Pagination());
        var (obj,next) = await _hookFactory.ExecuteBeforeQuery(_serviceProvider, occasion, EntityName, filters, sorts, pagination);
        Assert.Equal(Next.Exit,next);
        Assert.Equal(Name, ((People)obj!).Name);
    }

    [Fact]
    public async Task TestExecuteBeforeQuery()
    {
        var occasion = Occasion.AfterQueryMany;
        _hookFactory.AddHook(EntityName,occasion,Next.Continue, (Filters filters1, Sorts sorts1, Pagination pagination1) =>
        {
            filters1.Add(new Filter());
            sorts1.Add(new Sort());
            pagination1.Offset = 100;
        });
        
        var (filters, sorts, pagination) = (new Filters(), new Sorts(), new Pagination());
        await _hookFactory.ExecuteBeforeQuery(_serviceProvider, occasion, EntityName, filters, sorts, pagination);
        Assert.Single(filters);
        Assert.Single(sorts);
        Assert.Equal(100,pagination.Offset);

    }
    [Fact]
    public async Task TestAsyncReturnStringToObject()
    {
        var occasion = Occasion.BeforeQueryOne;
        _hookFactory.AddHook("people", occasion, Next.Continue,
            (string id) => new People { Id = Int32.Parse(id), Name = Name });

        _hookFactory.AddHook("people", occasion, Next.Continue,
            async (PeopleService service, People people) =>await service.ModifyPeopleAsyncReturn(people));

        var (res, _) = await _hookFactory.ExecuteStringToObject(_serviceProvider, occasion, "people", "1");
        var people = (People)res;
        Assert.Equal(1, people.Id);
        Assert.Equal(Name + "ModifyPeopleAsyncReturn", people.Name);
    }

    [Fact]
    public async Task TestAsyncStringToObject()
    {
        var occasion = Occasion.BeforeQueryOne;

        _hookFactory.AddHook(EntityName, occasion, Next.Continue,
            (string id) => new People { Id = Int32.Parse(id), Name = Name });

        _hookFactory.AddHook("people", occasion, Next.Continue,
            async (PeopleService service, People people) => await service.ModifyPeopleAsync(people));

        var (res, _) = await _hookFactory.ExecuteStringToObject(_serviceProvider, occasion, "people", "1");
        var people = (People)res;
        Assert.Equal(1, people.Id);
        Assert.Equal(Name + "ModifyPeopleAsync", people.Name);
    }

    [Fact]
    public async Task TestStringToObject()
    {
        var occasion = Occasion.BeforeQueryOne;
        
        _hookFactory.AddHook(EntityName, occasion, Next.Continue,
            (string id) => new People{Id = Int32.Parse(id), Name = Name});

        _hookFactory.AddHook(EntityName, occasion, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people));
        
        var (res,_ )= await _hookFactory.ExecuteStringToObject(_serviceProvider, occasion, "people", "1");
        var people = (People)res;
        Assert.Equal(1, people.Id);
        Assert.Equal(Name + "ModifyPeople", people.Name);
    }
    
    [Fact]
    public async Task TestRecordToObject()
    {
        var occasion = Occasion.AfterQueryOne;
        
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            ["name"] = Name
        };
        _hookFactory.AddHook(EntityName, occasion, Next.Continue,
            (PeopleService service, Record record) => service.ModifyRecord(record));

        _hookFactory.AddHook(EntityName, occasion, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people));
        var (res,_ )= await _hookFactory.ExecuteRecordToObject(_serviceProvider, occasion, "people", records);
        var people = (People)res;
        Assert.Equal(Name + "ModifyRecord" + "ModifyPeople", people.Name);
    }

    [Fact]
    public async Task TestRecordToRecordExit()
    {
        var name = "Alice";
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            ["name"] = name
        };

        _hookFactory.AddHook(EntityName, Occasion.BeforeInsert, Next.Exit,
            (PeopleService service, Record record) => service.ModifyRecord(record));
        
        _hookFactory.AddHook(EntityName, Occasion.BeforeInsert, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people));
        
        var (res,_ )= await _hookFactory.ExecuteRecordToRecord(_serviceProvider, Occasion.BeforeInsert, "people", records);
        Assert.Equal(name + "ModifyRecord" , res["name"]);
    }
    
    [Fact]
    public async Task TestRecordToRecord()
    {
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            ["name"] = Name
        };

        _hookFactory.AddHook(EntityName, Occasion.BeforeInsert, Next.Continue,
            (PeopleService service, Record record) => service.ModifyRecord(record));
        
        _hookFactory.AddHook(EntityName, Occasion.BeforeInsert, Next.Continue,
            (PeopleService service, People people) => service.ModifyPeople(people));
        
        var (res,_ )= await _hookFactory.ExecuteRecordToRecord(_serviceProvider, Occasion.BeforeInsert, "people", records);
        Assert.Equal(Name + "ModifyRecord" + "ModifyPeople", res["name"]);
    }
}