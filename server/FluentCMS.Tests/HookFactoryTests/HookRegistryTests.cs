using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using Moq;

namespace FluentCMS.Tests.HookFactoryTests;
using Record = IDictionary<string,object>;

public static class People
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
        _hookRegistry.EntityPostGetList.Register(People.EntityName,
            parameter => parameter with { RefListResult = parameter.RefListResult with { TotalRecords = 100 } });
        var res = new ListResult
        (
            Items:[],
            TotalRecords : 10
        );
        var args = new EntityPostGetListArgs(People.EntityName, res);
        args = await _hookRegistry.EntityPostGetList.Trigger(_serviceProvider,args );
        Assert.Equal(100, args.RefListResult.TotalRecords);
        
    }

    [Fact]
    public async Task TestEntityPreList()
    {
        var attr = new LoadedAttribute([], "", "");
        var entity = new LoadedEntity([], attr, attr, attr, "", ""); 
        _hookRegistry.EntityPreGetList.Register(People.EntityName, args => args);
        await _hookRegistry.EntityPreGetList.Trigger(_serviceProvider,
            new EntityPreGetListArgs(People.EntityName, entity ,[], [] , new ValidPagination(0,5)));
    }
    
    [Fact]
    public async Task TestModifyQuery()
    {
        var offset = 100;
        _hookRegistry.EntityPreGetList.Register(People.EntityName, args =>
             args with{ RefPagination = args.RefPagination with{Offset = offset}}
        );

        var attr = new LoadedAttribute([], "", "");
        var entity = new LoadedEntity([], attr, attr, attr, "", ""); 
        var args = new EntityPreGetListArgs(People.EntityName,entity, [], [], new ValidPagination(0,2));

        args = await _hookRegistry.EntityPreGetList.Trigger(_serviceProvider, args);
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