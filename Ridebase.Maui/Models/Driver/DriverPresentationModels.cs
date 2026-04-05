namespace Ridebase.Models.Driver;

public class DriverInsightModel
{
    public required string Title { get; set; }
    public required string Detail { get; set; }
    public required string AgeText { get; set; }
    public required string AccentColor { get; set; }
    public required string IconGlyph { get; set; }
}

public class DriverShortcutModel
{
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public required string AccentColor { get; set; }
    public required string IconGlyph { get; set; }
}

public class DriverStatSummaryModel
{
    public required string Value { get; set; }
    public required string Label { get; set; }
    public required string SupportingText { get; set; }
    public required string AccentColor { get; set; }
    public required string IconGlyph { get; set; }
}

public class DriverLeaderboardEntryModel
{
    public required string Name { get; set; }
    public required string RatingText { get; set; }
    public required string AmountText { get; set; }
}

public class DriverRatingMetricModel
{
    public required string Label { get; set; }
    public required string ScoreText { get; set; }
    public double Progress { get; set; }
}
