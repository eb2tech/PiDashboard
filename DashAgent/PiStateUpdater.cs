using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using MQTTnet;

namespace DashAgent;

internal class PiStateUpdater(IConfiguration configuration, ILogger<PiStateUpdater> logger) : BackgroundService
{
    private PiController PiController => field ??= new PiController(logger);
    private PiModel PiModel => field ??= new PiModel();

    private readonly string _mqttServer = configuration["Mqtt:Server"] ?? string.Empty;
    private readonly string _mqttUsername = configuration["Mqtt:Username"] ?? string.Empty;
    private readonly string _mqttPassword = configuration["Mqtt:Password"] ?? string.Empty;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(_mqttServer)
                                                              .WithCredentials(_mqttUsername, _mqttPassword)
                                                              .Build();

        mqttClient.ApplicationMessageReceivedAsync += (e) =>
        {
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            var topic = e.ApplicationMessage.Topic;

            return OnMqttMessage(mqttClient, payload, topic, stoppingToken);
        };

        var result = await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);
        if (!mqttClient.IsConnected)
        {
            logger.LogError("MQTT Client connected result: {ResultCode}, {Reason}", result.ResultCode,
                            result.ReasonString);
        }

        var topicPrefix = "raspi/" + PiController.DeviceId;
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                                              .WithTopicFilter(topicPrefix + "/backlight/set").Build();
        await mqttClient.SubscribeAsync(mqttSubscribeOptions, stoppingToken);

        await PublishDiscovery(mqttClient);

        while (!stoppingToken.IsCancellationRequested)
        {
            Poll();
            await Publish(mqttClient);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                                .Build();
        await mqttClient.DisconnectAsync(disconnectOptions, stoppingToken);
    }

    private Task OnMqttMessage(IMqttClient client, string payload, string topic, CancellationToken stoppingToken)
    {
        logger.LogInformation("Received Message: {Payload}", payload);
        logger.LogInformation("  on topic: {Topic}", topic);

        try
        {
            var json = JsonNode.Parse(payload);
            if (json != null)
            {
                var state = json["state"]!.GetValue<string>();
                switch (state)
                {
                    case "ON":
                    {
                        PiController.TurnOnDisplay();
                        var brightness = json["brightness"]?.GetValue<int>() ?? PiController.GetMaxBrightness();
                        PiController.SetBrightness(brightness);
                        break;
                    }
                    case "OFF":
                        PiController.TurnOffDisplay();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing backlight/set message");
        }

        _ = Task.Run(async () => await Publish(client), stoppingToken);

        return Task.CompletedTask;
    }

    private async Task Publish(IMqttClient mqttClient)
    {
        var piModel = PiModel;

        var backlightState = new JsonObject
                             {
                                 ["state"] = piModel.IsOn ? "ON" : "OFF",
                                 ["brightness"] = piModel.Brightness
                             };

        var topicPrefix = "raspi/" + PiController.DeviceId;

        var messages = new[]
                       {
                           new MqttApplicationMessageBuilder()
                               .WithTopic(topicPrefix + "/backlight/state")
                               .WithPayload(backlightState.ToJsonString())
                               .Build(),
                           new MqttApplicationMessageBuilder()
                               .WithTopic(topicPrefix + "/cpu/usage")
                               .WithPayload(piModel.CpuUsage.ToString())
                               .Build(),
                           new MqttApplicationMessageBuilder()
                               .WithTopic(topicPrefix + "/cpu/temperature")
                               .WithPayload(piModel.CpuTemp.ToString())
                               .Build(),
                           new MqttApplicationMessageBuilder()
                               .WithTopic(topicPrefix + "/memory/usage")
                               .WithPayload(piModel.MemoryUsage.ToString())
                               .Build()
                       };

        foreach (var message in messages)
        {
            await mqttClient.PublishAsync(message);
        }

        logger.LogDebug(
            "Published state - Brightness: {Brightness}, IsOn: {IsOn}, CPU: {CpuUsage}%, Temp: {CpuTemp}°C, Memory: {MemoryUsage}%",
            piModel.Brightness, piModel.IsOn, piModel.CpuUsage, piModel.CpuTemp, piModel.MemoryUsage);
    }

    private async Task PublishDiscovery(IMqttClient mqttClient)
    {
        var topicPrefix = "raspi/" + PiController.DeviceId;

        var root = new JsonObject
                   {
                       ["dev"] = new JsonObject
                                 {
                                     ["ids"] = PiController.DeviceId,
                                     ["name"] = "Raspberry Pi w/Display",
                                     ["mf"] = "Raspberry Pi Foundation",
                                     ["mdl"] = PiController.DeviceId
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
                                                                ["unique_id"] = PiController.DeviceId + "_backlight_01",
                                                                ["cmd_t"] = topicPrefix + "/backlight/set",
                                                                ["stat_t"] = topicPrefix + "/backlight/state",
                                                                //["bri_cmd_t"] = topicPrefix + "/backlight/brightness/set",
                                                                //["bri_stat_t"] = topicPrefix + "/backlight/brightness/state",
                                                                ["schema"] = "json",
                                                                ["brightness"] = true,
                                                                ["brightness_scale"] = PiController.GetMaxBrightness(),
                                                                ["name"] = "Backlight"
                                                            },
                                      ["cpu_usage"] = new JsonObject
                                                      {
                                                          ["p"] = "sensor",
                                                          ["unique_id"] = "raspi_cpu_usage_01",
                                                          ["stat_t"] = topicPrefix + "/cpu/usage",
                                                          ["unit_of_measurement"] = "%",
                                                          ["state_class"] = "measurement",
                                                          ["name"] = "CPU Usage"
                                                      },
                                      ["cpu_temperature"] = new JsonObject
                                                            {
                                                                ["p"] = "sensor",
                                                                ["unique_id"] = "raspi_cpu_temp_01",
                                                                ["stat_t"] = topicPrefix + "/cpu/temperature",
                                                                ["unit_of_measurement"] = "°C",
                                                                ["device_class"] = "temperature",
                                                                ["state_class"] = "measurement",
                                                                ["name"] = "CPU Temperature"
                                                            },
                                      ["memory_usage"] = new JsonObject
                                                         {
                                                             ["p"] = "sensor",
                                                             ["unique_id"] = "raspi_memory_usage_01",
                                                             ["stat_t"] = topicPrefix + "/memory/usage",
                                                             ["unit_of_measurement"] = "%",
                                                             ["state_class"] = "measurement",
                                                             ["name"] = "Memory Usage"
                                                         }
                                  }
                   };

        var json = root.ToJsonString(new JsonSerializerOptions
                                     {
                                         WriteIndented = false
                                     });

        var message = new MqttApplicationMessageBuilder()
                      .WithTopic("homeassistant/device/" + PiController.DeviceId + "/config")
                      .WithPayload(json)
                      .WithRetainFlag()
                      .Build();

        await mqttClient.PublishAsync(message);

        logger.LogInformation("Published MQTT discovery configuration for device " + PiController.DeviceId);
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