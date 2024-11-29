
using FluentResults;

namespace FluentCMS.Blog.Tests;

public static class ResultUtilExt
{
    public static T AssertSuccess<T>(this Result<T> result)
    {
        if (result.Errors.Count > 0)
        {
            throw new Exception(string.Join("\r\n", result.Errors));
        }
        return result.Value;
    }
    
    public static void AssertSuccess(this Result result)
    {
        if (result.Errors.Count > 0)
        {
            throw new Exception(string.Join("\r\n", result.Errors));
        }
    }
    
    public static void AssertFail<T>(this Result<T> result)
    {
        Assert.True(result.IsFailed);
    }

    public static void AssertFail(this Result result)
    {
        Assert.True(result.IsFailed);
    }
}