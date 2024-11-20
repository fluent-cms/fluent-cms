using FluentResults;

namespace FluentCMS.Utils.ResultExt;

public static class ResultExt
{
    public static Result BindAction<TValue>(this Result<TValue> res, Action<TValue> action)
    {
        return res.Bind(x =>
        {
            action(x);
            return Result.Ok();
        });
    }
}