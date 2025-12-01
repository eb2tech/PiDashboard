using System;

namespace PiDashboard.ViewModels;

using AdaptiveCards;

public class CardViewModel
{
    public AdaptiveCard Card { get; }

    public CardViewModel()
    {
        Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
               {
                   Body =
                   {
                       new AdaptiveTextBlock
                       {
                           Text = "Raspberry Pi Dashboard",
                           Size = AdaptiveTextSize.Large,
                           Weight = AdaptiveTextWeight.Bolder
                       },
                       new AdaptiveImage
                       {
                           Url = new Uri("https://raw.githubusercontent.com/microsoft/AdaptiveCards/main/samples/v1.0/Scenarios/FlightDetails.png"),
                           Size = AdaptiveImageSize.Medium
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
                           Title = "Check Logs",
                           Url = new Uri("http://localhost:8080/logs")
                       }
                   }
               };
    }
}
