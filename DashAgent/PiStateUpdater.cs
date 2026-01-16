using System.Text.Json;
using System.Text.Json.Nodes;
using MQTTnet;

namespace DashAgent;

public class PiStateUpdater(IConfiguration configuration) : BackgroundService
{
    private PiController PiController => field ??= new PiController();
    private PiModel PiModel => field ??= new PiModel();
    
    private readonly string _mqttServer = configuration["Mqtt:Server"] ?? "homeassistant2.local";
    private readonly string _mqttUsername = configuration["Mqtt:Username"] ?? string.Empty;
    private readonly string _mqttPassword = configuration["Mqtt:Password"] ?? string.Empty;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(_mqttServer)
                                                              .WithCredentials(_mqttUsername, _mqttPassword)
                                                              .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);

        await PublishDiscovery(mqttClient);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            Poll();
            await Publish(mqttClient);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        var disconnectOptions = new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                                                                        .Build();
        await mqttClient.DisconnectAsync(disconnectOptions, stoppingToken);
    }

    private async Task Publish(IMqttClient mqttClient)
    {
        var piModel = PiModel;

        var backlightState = new JsonObject
        {
            ["state"] = piModel.IsOn ? "ON" : "OFF",
            ["brightness"] = piModel.Brightness
        };

        var messages = new[]
        {
            new MqttApplicationMessageBuilder()
                .WithTopic("raspi/backlight/state")
                .WithPayload(backlightState.ToJsonString())
                .Build(),
            new MqttApplicationMessageBuilder()
                .WithTopic("raspi/backlight/brightness/state")
                .WithPayload(piModel.Brightness.ToString())
                .Build(),
            new MqttApplicationMessageBuilder()
                .WithTopic("raspi/cpu/usage")
                .WithPayload(piModel.CpuUsage.ToString())
                .Build(),
            new MqttApplicationMessageBuilder()
                .WithTopic("raspi/cpu/temperature")
                .WithPayload(piModel.CpuTemp.ToString())
                .Build(),
            new MqttApplicationMessageBuilder()
                .WithTopic("raspi/memory/usage")
                .WithPayload(piModel.MemoryUsage.ToString())
                .Build()
        };

        foreach (var message in messages)
        {
            await mqttClient.PublishAsync(message);
        }

        Console.WriteLine($"Published state - Brightness: {piModel.Brightness}, IsOn: {piModel.IsOn}, CPU: {piModel.CpuUsage}%, Temp: {piModel.CpuTemp}°C, Memory: {piModel.MemoryUsage}%");
    }

    private static async Task PublishDiscovery(IMqttClient mqttClient)
    {
        var root = new JsonObject
        {
            ["dev"] = new JsonObject
            {
                ["ids"] = "raspi_01",
                ["name"] = "Raspberry Pi",
                ["mf"] = "Raspberry Pi Foundation",
                ["mdl"] = "Raspberry Pi"
            },
            ["o"] = new JsonObject
            {
                ["name"] = "raspi2mqtt",
                ["sw"] = "1.0",
                ["url"] = "https://example.local"
            },
            ["cmps"] = new JsonObject
            {
                ["raspi_backlight"] = new JsonObject
                {
                    ["p"] = "light",
                    ["unique_id"] = "raspi_backlight_01",
                    ["cmd_t"] = "raspi/backlight/set",
                    ["stat_t"] = "raspi/backlight/state",
                    ["bri_cmd_t"] = "raspi/backlight/brightness/set",
                    ["bri_stat_t"] = "raspi/backlight/brightness/state",
                    ["schema"] = "json",
                    ["brightness"] = true
                },
                ["cpu_usage"] = new JsonObject
                {
                    ["p"] = "sensor",
                    ["unique_id"] = "raspi_cpu_usage_01",
                    ["stat_t"] = "raspi/cpu/usage",
                    ["unit_of_measurement"] = "%",
                    ["device_class"] = "power_factor",
                    ["state_class"] = "measurement"
                },
                ["cpu_temperature"] = new JsonObject
                {
                    ["p"] = "sensor",
                    ["unique_id"] = "raspi_cpu_temp_01",
                    ["stat_t"] = "raspi/cpu/temperature",
                    ["unit_of_measurement"] = "°C",
                    ["device_class"] = "temperature",
                    ["state_class"] = "measurement"
                },
                ["memory_usage"] = new JsonObject
                {
                    ["p"] = "sensor",
                    ["unique_id"] = "raspi_memory_usage_01",
                    ["stat_t"] = "raspi/memory/usage",
                    ["unit_of_measurement"] = "%",
                    ["device_class"] = "power_factor",
                    ["state_class"] = "measurement"
                }
            }
        };

        var json = root.ToJsonString(new JsonSerializerOptions
                                     {
                                         WriteIndented = false
                                     });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("homeassistant/device/raspi_01/config")
            .WithPayload(json)
            .WithRetainFlag()
            .Build();

        await mqttClient.PublishAsync(message);

        Console.WriteLine($"Published discovery to MQTT: {json}");
    }

    private void Poll()
    {
        var piController = PiController;
        var piModel = PiModel;

        piModel.Brightness = piController.GetBrightness();
        piModel.CpuTemp = piController.GetCpuTemp();
        piModel.CpuUsage = piController.GetCpuUsage();
        piModel.IsOn = piController.IsDisplayOn();
        piModel.MemoryUsage = piController.GetMemoryUsage();
    }
}