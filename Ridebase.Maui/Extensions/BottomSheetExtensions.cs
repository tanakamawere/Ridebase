using WhatIsThisSheet;

namespace Ridebase.Extensions;

public static class BottomSheetExtensions
{
    // Method to get the current percentage of the sheet
    public static double GetCurrentSheetPercentage(this BottomSheet bottomSheet)
    {
        ArgumentNullException.ThrowIfNull(bottomSheet);

        double visibleHeight = bottomSheet.Height;
        double currentTranslationY = bottomSheet.TranslationY;

        // Calculate the percentage displayed
        double percentageDisplayed = 1.0 - (currentTranslationY / visibleHeight);
        return Math.Max(0, Math.Min(percentageDisplayed, 1.0)); // Clamp between 0 and 1
    }
}
