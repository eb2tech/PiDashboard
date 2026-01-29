using System;
using System.Reactive.Linq;
using AdaptiveCards;
using AdaptiveCards.Templating;
using ReactiveUI;

namespace PiDashboard.ViewModels;

public class CardViewModel : ReactiveObject
{
    private AdaptiveCard? _card;
    private readonly string _cardTemplate;
    private float _openingPrice;

    public AdaptiveCard? Card
    {
        get => _card;
        private set => this.RaiseAndSetIfChanged(ref _card, value);
    }

    public CardViewModel()
    {
        _cardTemplate = """
                        {
                           	"type": "AdaptiveCard",
                           	"$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                           	"version": "1.5",
                           	"body": [
                           		{
                           			"type": "TextBlock",
                           			"text": "Stock Update",
                           			"weight": "bolder",
                           			"size": "medium",
                           			"style": "heading",
                           			"wrap": true
                           		},
                           		{
                           			"type": "ColumnSet",
                           			"columns": [
                           				{
                           					"type": "Column",
                           					"width": "auto",
                           					"items": [
                           						{
                           							"type": "Image",
                           							"url": "${logo}",
                           							"altText": "${CompanyName} Logo",
                           							"size": "small"
                           						}
                           					]
                           				},
                           				{
                           					"type": "Column",
                           					"width": "stretch",
                           					"items": [
                           						{
                           							"type": "TextBlock",
                           							"text": "${Symbol}",
                           							"weight": "bolder",
                           							"wrap": true
                           						},
                           						{
                           							"type": "TextBlock",
                           							"text": "${CompanyName}",
                           							"isSubtle": true,
                           							"spacing": "none",
                           							"wrap": true
                           						}
                           					]
                           				}
                           			]
                           		},
                           		{
                           			"type": "FactSet",
                           			"facts": [
                           				{
                           					"title": "Current Price",
                           					"value": "${currentPrice}"
                           				},
                           				{
                           					"title": "Updated At",
                           					"value": "${updatedAt}"
                           				}
                           			]
                           		},
                           		{
                           			"type": "ColumnSet",
                           			"columns": [
                           				{
                           					"type": "Column",
                           					"width": "stretch",
                           					"items": [
                           						{
                           							"type": "TextBlock",
                           							"text": "24h High",
                           							"isSubtle": true,
                           							"wrap": true
                           						},
                           						{
                           							"type": "TextBlock",
                           							"text": "${high}",
                           							"size": "large",
                           							"spacing": "small",
                           							"wrap": true
                           						}
                           					]
                           				},
                           				{
                           					"type": "Column",
                           					"width": "stretch",
                           					"items": [
                           						{
                           							"type": "TextBlock",
                           							"text": "24h Low",
                           							"isSubtle": true,
                           							"horizontalAlignment": "center",
                           							"wrap": true
                           						},
                           						{
                           							"type": "TextBlock",
                           							"text": "${low}",
                           							"size": "large",
                           							"horizontalAlignment": "center",
                           							"spacing": "small",
                           							"wrap": true
                           						}
                           					]
                           				},
                           				{
                           					"type": "Column",
                           					"width": "stretch",
                           					"items": [
                           						{
                           							"type": "TextBlock",
                           							"text": "Change",
                           							"isSubtle": true,
                           							"horizontalAlignment": "right",
                           							"wrap": true
                           						},
                           						{
                           							"type": "TextBlock",
                           							"text": "${change}",
                           							"size": "large",
                           							"color": "${changeColor}",
                           							"horizontalAlignment": "right",
                           							"spacing": "small",
                           							"wrap": true
                           						}
                           					]
                           				}
                           			]
                           		}
                           	],
                           	"actions": [
                           		{
                           			"type": "Action.OpenUrl",
                           			"title": "View Stock Chart",
                           			"url": "${chartUrl}"
                           		}
                           	]
                        }
                        """;

        _openingPrice = Random.Shared.Next(70, 80) + Random.Shared.Next(10, 99) / 100f;
        RefreshCard();

        // Optional: Auto-refresh every minute
        Observable.Interval(TimeSpan.FromMinutes(1))
                  .Subscribe(_ => RefreshCard());
    }

    public void RefreshCard()
    {
        var currentPrice = Random.Shared.Next(70, 80) + Random.Shared.Next(10, 99) / 100f;
        var priceChange = currentPrice - _openingPrice;
        var formattedChange = priceChange < 0 
            ? $"({Math.Abs(priceChange):F2})" 
            : $"{priceChange:F2}";

        var data = new
                   {
                       Symbol = "MSFT",
                       CompanyName = "Microsoft Corp",
                       logo = "https://adaptivecards.io/content/img/microsof-logo.png",
                       currentPrice = $"${currentPrice:F2}",
                       updatedAt = DateTime.Now.ToString("hh:mm tt"),
                       high = "$77.92",
                       low = "$75.24",
                       change = formattedChange,
                       changeColor = priceChange > 0 ? "good" : "attention",
                       chartUrl = "http://finance.yahoo.com/q?s=MSFT"
                   };

        var template = new AdaptiveCardTemplate(_cardTemplate);
        var cardJson = template.Expand(data);
        Card = AdaptiveCard.FromJson(cardJson).Card;
    }
}