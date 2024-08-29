using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using Moq;

namespace FluentCMS.Tests.HookFactoryTests;
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
}

public class HookRegistryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HookRegistry _hookRegistry;

    public HookRegistryTests()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(PeopleService)))
            .Returns(new PeopleService());

        _serviceProvider = mockServiceProvider.Object;
        _hookRegistry = new HookRegistry();
    }

    [Fact]
    public async Task TestExecuteAfterQuery()
    {
        var occasion = Occasion.AfterReadList;
        _hookRegistry.AddHooks(People.EntityName, [occasion], (ListResult res) => { res.TotalRecords = 100; });

        var res = new ListResult
        {
            TotalRecords = 10,
        };
        var meta = new EntityMeta(People.EntityName, "3");
         
        await _hookRegistry.Trigger(_serviceProvider, occasion,meta, new HookParameter{ListResult = res});
        Assert.Equal(100, res.TotalRecords);
    }

    [Fact]
    public async Task TestExecuteBeforeQueryReplace()
    {
        var occasion = Occasion.AfterReadList;
        _hookRegistry.AddHooks(People.EntityName, [occasion], () => new People
        {
            id = 3, Name = People.NameValue
        });
        var meta = new EntityMeta(People.EntityName, "3");

        var (filters, sorts, pagination) = (new Filters(), new Sorts(), new Pagination());
        var hookData = new HookParameter
        {
            Filters = filters,
            Sorts = sorts,
            Pagination = pagination
        };
        await _hookRegistry.Trigger(_serviceProvider, occasion, meta, hookData);
    }
    [Fact]
    public async Task TestModifyQuery()
    {
        var occasion = Occasion.AfterReadList;
        _hookRegistry.AddHooks(People.EntityName, [occasion],
            (Filters filters1, Sorts sorts1, Pagination pagination1) =>
            {
                filters1.Add(new Filter());
                sorts1.Add(new Sort());
                pagination1.Offset = 100;
            });
        var meta = new EntityMeta(People.EntityName );
        var (filters, sorts, pagination) = (new Filters(), new Sorts(), new Pagination());
        var hookData = new HookParameter
        {
            Filters = filters,
            Sorts = sorts,
            Pagination = pagination
        };
        await _hookRegistry.Trigger(_serviceProvider, occasion, meta, hookData);
        Assert.Single(filters);
        Assert.Single(sorts);
        Assert.Equal(100, pagination.Offset);

    }
    [Fact]
    public async Task TestModifyRecord()
    {
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            [People.NameFieldName] = People.NameValue
        };

        _hookRegistry.AddHooks(People.EntityName, [Occasion.BeforeInsert],
            (PeopleService service, Record record) => service.ModifyRecord(record, "ModifyRecord"));

        var meta = new EntityMeta(People.EntityName, "1");
        await _hookRegistry.Trigger(_serviceProvider, Occasion.BeforeInsert, meta, new HookParameter{Record = records});
    }
}