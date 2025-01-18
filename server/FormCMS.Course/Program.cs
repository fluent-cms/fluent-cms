using FormCMS.Course;

var webApp = await WebApp.Build(args);
var worker = HostApp.Build(args);
await Task.WhenAll( webApp.RunAsync(), worker.RunAsync());