namespace FluentCMS.Utils.HookFactory;

public abstract record BaseArgs(string Name)
{
    public Dictionary<string, object> Context { get; } = new();
}