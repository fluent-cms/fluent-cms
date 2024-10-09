namespace FluentCMS.Utils.HookFactory;
public class HookException (string message): Exception(message);
public class NullExceptionBuilder<T>(T? val)
{
    public T ValOrThrow(string message)
    {
        if (val is null)
        {
            throw new HookException(message);
        }
        return val;
    }
}
public static class HookChecker
{
    public  static NullExceptionBuilder<T> NotNull<T>( T? obj)
    {
        return new NullExceptionBuilder<T>(obj);
    }
} 
