using Microsoft.Extensions.Logging;
using Ridebase.Models.Driver;
using System.Collections.ObjectModel;

namespace Ridebase.ViewModels.Driver;

public class DriverStatsViewModel : BaseViewModel
{
    public string EarningsHeadline => "Earnings & Insights";
    public string EarningsTotal => "$1,284.50";
    public string EarningsBaseline => "$942.20";
    public string EarningsStretch => "$342.30";
    public string OverallRating => "4.92";
    public string RatingCaption => "Based on recent trip reviews";

    public ObservableCollection<DriverStatSummaryModel> SummaryCards { get; } =
    [
        new DriverStatSummaryModel
        {
            Value = "142",
            Label = "Rides Completed",
            SupportingText = "+12%",
            AccentColor = "#CCE8E7",
            IconGlyph = "\uf1b9"
        },
        new DriverStatSummaryModel
        {
            Value = "38.5",
            Label = "Hours Online",
            SupportingText = "Active",
            AccentColor = "#F6E3D7",
            IconGlyph = "\uf017"
        }
    ];

    public ObservableCollection<DriverLeaderboardEntryModel> TopEarners { get; } =
    [
        new DriverLeaderboardEntryModel { Name = "Takunda M.", RatingText = "4.9", AmountText = "$1,840" },
        new DriverLeaderboardEntryModel { Name = "Chipo N.", RatingText = "4.8", AmountText = "$1,612" },
        new DriverLeaderboardEntryModel { Name = "Robert C.", RatingText = "4.7", AmountText = "$1,550" }
    ];

    public ObservableCollection<DriverRatingMetricModel> RatingMetrics { get; } =
    [
        new DriverRatingMetricModel { Label = "Safety", ScoreText = "4.9", Progress = 0.98 },
        new DriverRatingMetricModel { Label = "Service", ScoreText = "4.7", Progress = 0.94 },
        new DriverRatingMetricModel { Label = "Vehicle", ScoreText = "4.8", Progress = 0.96 }
    ];

    public DriverStatsViewModel(ILogger<DriverStatsViewModel> logger)
    {
        Logger = logger;
        Logger.LogInformation("DriverStatsViewModel initialized");
    }
}
