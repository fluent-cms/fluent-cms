namespace FormCMS.Core.HookFactory;

public abstract record BaseArgs(string Name)
{
    public Dictionary<string, object> Context { get; } = new();
}