using System.Runtime.InteropServices;
using System.Text;
using DashAgent.Parsers;

namespace DashAgent
{
    internal class PiController
    {
        // Check your specific OS/device for the exact path
        private const string BacklightPath = "/sys/class/backlight/11-0045";
        private static string BrightnessFile => Path.Combine(BacklightPath, "brightness");
        private static string MaxBrightnessFile => Path.Combine(BacklightPath, "max_brightness");

        private const int ClockTicksPerSecond = 100;
        private ProcStatParser.CpuUsageData? _lastCpuData;
        private DateTime _lastCpuReadTime;

        public static string DeviceId => throw new NotImplementedException();

        public int GetMaxBrightness()
        {
            if (File.Exists(MaxBrightnessFile))
            {
                var brightnessFile = File.ReadAllText(MaxBrightnessFile).Trim();
                if (int.TryParse(brightnessFile, out int maxBrightness))
                    return maxBrightness;
            }

            return 31; // Default if parsing fails
        }

        public int GetBrightness()
        {
            if (File.Exists(BrightnessFile))
            {
                try
                {
                    var brightnessValue = File.ReadAllText(BrightnessFile).Trim();
                    if (int.TryParse(brightnessValue, out int brightness))
                        return brightness;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading brightness: {ex.Message}");
                }
            }

            return 0; // Default if file doesn't exist or parsing fails
        }

        public void SetBrightness(int value)
        {
            int max = GetMaxBrightness();
            if (value < 0) value = 0;
            if (value > max) value = max;

            try
            {
                File.WriteAllText(BrightnessFile, value.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting brightness: {ex.Message}");
            }
        }

        // Helper to turn off the display by setting brightness to 0
        public void TurnOffDisplay() => SetBrightness(0);

        // Helper to turn on the display by setting brightness to max
        public void TurnOnDisplay() => SetBrightness(GetMaxBrightness());

        public static bool IsRunningOnPi()
        {
            // Detect whether we're running on a Raspberry Pi or not.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Not running on Linux (not a Raspberry Pi). Exiting.");
                return false;
            }

            string? model = null;
            const string modelPath = "/proc/device-tree/model";
            if (File.Exists(modelPath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(modelPath);
                    model = Encoding.UTF8.GetString(bytes).Trim('\0', '\r', '\n');
                }
                catch
                {
                    // ignore and fall back
                }
            }

            if (string.IsNullOrEmpty(model))
            {
                try
                {
                    var cpuinfo = File.ReadAllText("/proc/cpuinfo");
                    foreach (var line in cpuinfo.Split('\n'))
                    {
                        if (line.StartsWith("Model", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("Hardware", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                model = parts[1].Trim();
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            var isPi = !string.IsNullOrEmpty(model) && (
                model.Contains("Raspberry", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("BCM") ||
                model.Contains("RPI", StringComparison.OrdinalIgnoreCase)
            );

            return isPi;
        }

        public uint GetCpuTemp()
        {
            const string thermalZonePath = "/sys/class/thermal/thermal_zone0/temp";
            
            if (File.Exists(thermalZonePath))
            {
                try
                {
                    var tempValue = File.ReadAllText(thermalZonePath).Trim();
                    if (int.TryParse(tempValue, out int milliDegrees))
                    {
                        return (uint)(milliDegrees / 1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading CPU temperature: {ex.Message}");
                }
            }

            return 0;
        }

        public uint GetCpuUsage()
        {
            const string procStatPath = "/proc/stat";
            
            if (!File.Exists(procStatPath))
            {
                return 0;
            }

            try
            {
                var currentTime = DateTime.UtcNow;
                var stats = File.ReadAllText(procStatPath);
                var cpuData = ProcStatParser.ParseCpuUsage(stats, ClockTicksPerSecond).FirstOrDefault();

                if (_lastCpuData == null)
                {
                    _lastCpuData = cpuData;
                    _lastCpuReadTime = currentTime;
                    return 0;
                }

                var timeDelta = (currentTime - _lastCpuReadTime).TotalSeconds;
                if (timeDelta < 0.1)
                {
                    return 0;
                }

                var prevData = _lastCpuData.Value;
                
                var totalDelta = (cpuData.User - prevData.User) +
                                (cpuData.Nice - prevData.Nice) +
                                (cpuData.System - prevData.System) +
                                (cpuData.Idle - prevData.Idle) +
                                (cpuData.IoWait - prevData.IoWait) +
                                (cpuData.Irq - prevData.Irq) +
                                (cpuData.SoftIrq - prevData.SoftIrq) +
                                (cpuData.Steal - prevData.Steal);

                var idleDelta = cpuData.Idle - prevData.Idle;
                
                var usage = totalDelta > 0 ? (1.0 - (idleDelta / totalDelta)) * 100.0 : 0.0;

                _lastCpuData = cpuData;
                _lastCpuReadTime = currentTime;

                return (uint)Math.Clamp(usage, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CPU usage: {ex.Message}");
                return 0;
            }
        }

        public bool IsDisplayOn()
        {
            return GetBrightness() > 0;
        }

        public uint GetMemoryUsage()
        {
            try
            {
                var memInfo = MemInfoParser.Parse().ToDictionary(x => x.field, x => x.value);

                if (!memInfo.TryGetValue("MemTotal", out var memTotal) || memTotal == 0)
                {
                    return 0;
                }

                if (!memInfo.TryGetValue("MemAvailable", out var memAvailable))
                {
                    if (memInfo.TryGetValue("MemFree", out var memFree))
                    {
                        memAvailable = memFree;
                    }
                    else
                    {
                        return 0;
                    }
                }

                var memUsed = memTotal - memAvailable;
                var usagePercent = (double)memUsed / memTotal * 100.0;

                return (uint)Math.Clamp(usagePercent, 0, 100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading memory usage: {ex.Message}");
                return 0;
            }
        }
    }
}