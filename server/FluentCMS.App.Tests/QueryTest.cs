using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.Test;

namespace FluentCMS.App.Tests;

//have to start FluentCMS.App.AppHost manually
public class QueryTest
{
    private readonly BlogsTestCases _blogsTest =
        new (
            new QueryApiClient(
                new HttpClient { BaseAddress = new Uri("http://localhost:5001") }
            ),
            "post_test");

    [Fact]
    public Task AllBlogTest() => _blogsTest.RunAll();
}