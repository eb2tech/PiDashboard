# Implementation Notes

---

# Raspberry Pi Dashboard with Avalonia + Adaptive Cards

This project demonstrates how to build a **cross-platform dashboard** on Raspberry Pi using **.NET C#**, **Avalonia UI**, 
and the **Adaptive Cards renderer**. Adaptive Cards provide a schema-driven way to define UI in JSON, and Avalonia makes it 
possible to render them natively across Linux, macOS, Windows, and Raspberry Pi.

---

## 🚀 Prerequisites

- Raspberry Pi 2 or newer (ARMv7+)
- Raspberry Pi OS or other Linux distro
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Avalonia templates:
  ```bash
  dotnet new install Avalonia.Templates
  ```

---

## 🛠️ Project Setup

1. Create a new Avalonia app:
   ```bash
   dotnet new avalonia.app -o PiDashboard
   cd PiDashboard
   ```

2. Add the Adaptive Cards Avalonia renderer:
   ```bash
   dotnet add package Iciclecreek.AdaptiveCards.Rendering.Avalonia
   ```

---

## 📝 Example: Single Card Dashboard

### ViewModel
```csharp
using AdaptiveCards;

public class CardViewModel
{
    public AdaptiveCard Card { get; }

    public CardViewModel()
    {
        Card = new AdaptiveCard(new AdaptiveSchemaVersion(1,5))
        {
            Body =
            {
                new AdaptiveTextBlock
                {
                    Text = "Raspberry Pi Dashboard",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder
                },
                new AdaptiveTextBlock
                {
                    Text = $"System Time: {DateTime.Now}",
                    Wrap = true
                }
            },
            Actions =
            {
                new AdaptiveOpenUrlAction
                {
                    Title = "View Logs",
                    Url = new Uri("http://localhost:8080/logs")
                }
            }
        };
    }
}
```

### XAML
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:ac="using:AdaptiveCards.Rendering.Avalonia"
        Title="Pi Dashboard" Width="800" Height="600">
    <StackPanel>
        <ac:AdaptiveCardView Card="{Binding Card}" Width="600"
                             Action="OnAdaptiveAction"/>
    </StackPanel>
</Window>
```

### Action Handler
```csharp
using System.Diagnostics;
using AdaptiveCards.Rendering.Avalonia;

public static async void OnAdaptiveAction(Window window, object? sender, RoutedAdaptiveActionEventArgs e)
{
    if (e.Action is AdaptiveOpenUrlAction openUrlAction)
    {
        Process.Start(new ProcessStartInfo(openUrlAction.Url.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
    else if (e.IsDataAction())
    {
        Console.WriteLine("Data submitted!");
        e.Handled = true;
    }
}
```

---

## 🖼️ Multi-Card Dashboard Layout

You can scale this into a **multi-card dashboard** by stacking or arranging multiple `AdaptiveCardView` controls inside a grid or panel. For example:

```xml
<Grid ColumnDefinitions="*,*">
    <!-- System Health Card -->
    <ac:AdaptiveCardView Card="{Binding SystemHealthCard}" Grid.Column="0" Width="400"/>

    <!-- Network Status Card -->
    <ac:AdaptiveCardView Card="{Binding NetworkStatusCard}" Grid.Column="1" Width="400"/>

    <!-- IoT Sensor Data Card -->
    <ac:AdaptiveCardView Card="{Binding SensorCard}" Grid.Column="0" Grid.Row="1" Width="400"/>

    <!-- Logs / Alerts Card -->
    <ac:AdaptiveCardView Card="{Binding AlertsCard}" Grid.Column="1" Grid.Row="1" Width="400"/>
</Grid>
```

Each card can be bound to its own `AdaptiveCard` instance in your ViewModel:

```csharp
public AdaptiveCard SystemHealthCard { get; }
public AdaptiveCard NetworkStatusCard { get; }
public AdaptiveCard SensorCard { get; }
public AdaptiveCard AlertsCard { get; }
```

This allows you to modularize your dashboard:
- **System Health** → CPU, memory, temperature
- **Network Status** → IP, bandwidth, connectivity
- **IoT Sensors** → readings from GPIO, ESP32, or MQTT
- **Alerts/Logs** → recent events, warnings, or notifications

---

## ⚡ Deployment

Publish for Raspberry Pi ARM:

```bash
dotnet publish -r linux-arm -c Release --self-contained
```

Copy the build to your Pi and run:

```bash
./PiDashboard
```

---

## ✅ Next Steps

- Add **dynamic data sources** (system metrics, APIs, IoT sensors).
- Style cards with **Adaptive Host Config** for consistent theming.
- Extend with **Action.Submit** to trigger backend logic.
- Use **WebView fallback** if you want full Adaptive Cards JS renderer features.

---

## 📚 References

- [Avalonia UI](https://avaloniaui.net/)
- [Adaptive Cards](https://adaptivecards.io/)
- [Iciclecreek Adaptive Cards Avalonia Renderer](https://github.com/tomlm/Iciclecreek.AdaptiveCards.Rendering.Avalonia)

