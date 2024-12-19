using FluentCMS.App;

var webApp = await WebApp.Build(args);
webApp.Run();
var hostApp = HostApp.Build(args);
await Task.WhenAll(webApp.RunAsync(), hostApp.RunAsync());