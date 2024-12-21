using FluentCMS.App;

var webApp = await WebApp.Build(args);
var hostApp = HostApp.Build(args);
await Task.WhenAll(webApp?.RunAsync()??Task.CompletedTask, hostApp?.RunAsync()??Task.CompletedTask);