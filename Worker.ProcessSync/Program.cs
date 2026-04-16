using Worker.ProcessSync.Infrastructure;
using Worker.ProcessSync.Interfaces;
using Worker.ProcessSync.Persistence;
using Worker.ProcessSync.Services;
using Worker.ProcessSync.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<IProcessSyncService, ProcessSyncService>();
builder.Services.AddScoped<ICamundaClient, CamundaClient>();
builder.Services.AddScoped<IProcessRepository, ProcessRepository>();

builder.Services.AddHostedService<ProcessSyncWorker>();

var app = builder.Build();
app.Run();