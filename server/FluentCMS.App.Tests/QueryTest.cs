using FluentCMS.CoreKit.ApiClient;
using FluentCMS.CoreKit.Test;

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