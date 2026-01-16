using DashAgent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddSingleton<PiController>();
builder.Services.AddHostedService<PiStateUpdater>();

var app = builder.Build();

using var factory = LoggerFactory.Create(b => b.AddConsole());
var logger = factory.CreateLogger<Program>();

logger.LogInformation("Starting application...");

var isPi = PiController.IsRunningOnPi();
if (!isPi)
{
    logger.LogError("Not running on a Raspberry Pi. Exiting.");
    Environment.Exit(1);
}

await app.RunAsync();