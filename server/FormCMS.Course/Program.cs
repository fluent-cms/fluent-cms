using FormCMS.Course;

var webApp = await Web.Build(args);
var worker = Worker.Build(args);
await Task.WhenAll( webApp.RunAsync(), worker?.RunAsync()??Task.CompletedTask);