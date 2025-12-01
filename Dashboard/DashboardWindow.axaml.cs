using AdaptiveCards;
using AdaptiveCards.Rendering.Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using PiDashboard.ViewModels;
using System;
using System.Diagnostics;

namespace PiDashboard;

public partial class DashboardWindow : Window
{
    public DashboardWindow()
    {
        InitializeComponent();
        DataContext = new CardViewModel();
    }

    private void OnAdaptiveAction(object? sender, RoutedAdaptiveActionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Handle adaptive card actions here. Log the action title.
        Logger.TryGet(LogEventLevel.Information, "PiDashboard")?.Log(this, $"Adaptive action invoked: {e.Action.Title}");

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
}
