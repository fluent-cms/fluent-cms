namespace FluentCMS.Utils.HookFactory;

public class HookList<TArgs>
    where TArgs : BaseArgs
{
    private readonly List<Hook<TArgs>> _hooks = new ();
    public void RegisterDynamic(string schemaName, Delegate func)
    {
        _hooks.Add(new Hook<TArgs>
        {
            SchemaName = schemaName,
            Callback = func
        });
    }
    public void RegisterAsync(string schemaName, Func<TArgs, Task<TArgs>> func) => RegisterDynamic(schemaName, func);
    public void Register(string schemaName, Func<TArgs, TArgs> func) => RegisterDynamic(schemaName, func);
    public async Task<TArgs> Trigger(IServiceProvider provider, TArgs args)
    {
        foreach (var hook in _hooks.Where(x => StartsWith(x.SchemaName, args.Name)))
        {
            args = await hook.Trigger(provider, args);
        }
        return args;
    }

    private bool StartsWith(string str, string prefix)
    {
        if (str == prefix || prefix == "*")
        {
            return true;
        }

        if (prefix.EndsWith("*"))
        {
            prefix = prefix.Substring(0, prefix.Length - 1);
            return str.StartsWith(prefix);
        }
        return false;
    }
}
