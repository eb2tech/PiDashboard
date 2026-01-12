using DashAgent;

var isPi = PiController.IsRunningOnPi();
if (!isPi)
{
    Console.WriteLine("Not running on a Raspberry Pi. Exiting.");
    Environment.Exit(1);
}

Console.WriteLine($"Hello, World! Running on Raspberry Pi.");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PiController>();

var app = builder.Build();

await app.RunAsync();