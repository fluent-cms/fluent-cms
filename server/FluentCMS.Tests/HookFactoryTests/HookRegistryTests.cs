using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace FluentCMS.Tests.HookFactoryTests;
using Record = IDictionary<string,object>;

public class People
{
    public const string EntityName = "people";
    public const string NameFieldName = "Name";
    public const string NameValue = "Alice";
}

public class HookRegistryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HookRegistry _hookRegistry;

    public HookRegistryTests()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        _serviceProvider = mockServiceProvider.Object;
        _hookRegistry = new HookRegistry();
    }

    [Fact]
    public async Task TestEntityPostListChangeTotal()
    {
        _hookRegistry.EntityPostGetList.Register(People.EntityName, parameter =>
        {
            parameter.RefListResult.TotalRecords = 100;
            parameter.Context.Add("abc",1);
            return parameter;

        });
        var res = new ListResult
        {
            TotalRecords = 10,
        };
        var args = new EntityPostGetListArgs(People.EntityName, res);
        await _hookRegistry.EntityPostGetList.Trigger(_serviceProvider,args );
        Assert.Equal(100, res.TotalRecords);
        Assert.Equal(1, (int)args.Context["abc"]);
        
    }

    [Fact]
    public async Task TestEntityPreList()
    {
        _hookRegistry.EntityPreGetList.Register(People.EntityName, args => args);
        await _hookRegistry.EntityPreGetList.Trigger(_serviceProvider,
            new EntityPreGetListArgs(People.EntityName, new Filters(), new Sorts(), new Pagination()));
    }
    
    [Fact]
    public async Task TestModifyQuery()
    {
        var offset = 100;
        _hookRegistry.EntityPreGetList.Register(People.EntityName, args =>
        {
            args.RefPagination.Offset = offset;
            args.RefFilters.Add(new Filter());
            args.RefSorts.Add(new Sort());
            return args;
        });

        var args = new EntityPreGetListArgs(People.EntityName, [], [], new Pagination());

        await _hookRegistry.EntityPreGetList.Trigger(_serviceProvider, args);
        Assert.Single(args.RefFilters);
        Assert.Single(args.RefSorts);
        Assert.Equal(offset, args.RefPagination.Offset);
    }
    [Fact]
    public async Task TestEntityPreAddModifyRecord()
    {
        Record records = new Dictionary<string, object>
        {
            ["id"] = 1,
            [People.NameFieldName] = People.NameValue
        };
        _hookRegistry.EntityPreAdd.Register(People.EntityName, args =>
        {
            args.RefRecord[People.NameFieldName] = People.NameValue + "1";
            return args;
        });
        await _hookRegistry.EntityPreAdd.Trigger(_serviceProvider, new EntityPreAddArgs(People.EntityName, records));
    }
}