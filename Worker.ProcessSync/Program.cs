using Worker.ProcessSync.Config;

var builder = Host.CreateApplicationBuilder(args);

builder.AddWorkerServices();

var app = builder.Build();
app.Run();