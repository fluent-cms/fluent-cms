
using FluentResults;

namespace FluentCMS.IntegrationTests;

public static class ResultUtilExt
{
    public static T AssertSuccess<T>(this Result<T> result)
    {
        Assert.True(result.IsSuccess);
        return result.Value;
    }
    
    public static void AssertSuccess(this Result result)
    {
        if (result.Errors.Count > 0)
        {
            throw new Exception(string.Join("\r\n", result.Errors));
        }
        
        Assert.True(result.IsSuccess);
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