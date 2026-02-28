# UI Styles & Reusability Guidelines

## Core Rule

When doing any UI work, **always check for existing reusable styles first** before writing inline property values. If a suitable style doesn't exist, **create one** so it can be reused across the app.

## Before Writing UI Code

1. **Check `Resources/Styles/RidebaseStyles.xaml`** for app-specific reusable styles (e.g., `BubbleBorder`, `BubbleLabel`).
2. **Check `Resources/Styles/Styles.xaml`** for default MAUI implicit styles.
3. **Check `Resources/Styles/Colors.xaml`** for color definitions — always use `{StaticResource ColorName}` instead of hardcoded hex values.
4. **Check page-level `ResourceDictionary`** sections for any local styles or resources already defined on the page you're editing.

## When to Create a New Style

Create a reusable style in `Resources/Styles/RidebaseStyles.xaml` when:

- The same combination of properties (padding, background, corner radius, font size, etc.) appears on **2 or more elements** across different pages.
- You're building a UI pattern that will likely be reused (cards, menu items, floating buttons, pills/badges, section headers).

## How to Define Styles

- Use **explicit styles** (`x:Key="StyleName"`) for component-specific patterns.
- Use **implicit styles** (no `x:Key`, just `TargetType`) only for broad defaults that should apply everywhere.
- Name styles descriptively: `FlyoutMenuItem`, `FloatingActionButton`, `CardBorder`, `SectionHeaderLabel`, etc.
- Group related styles together with XML comments.

## Color Usage

- **Never hardcode hex colors** in XAML pages. Define them in `Colors.xaml` and reference via `{StaticResource}`.
- Use `{AppThemeBinding Light=..., Dark=...}` for theme-aware colors.
- The only exception is one-off opacity overlays like `#40FFFFFF` used in a single place.

## Example

```xml
<!-- Bad: inline properties repeated across pages -->
<Border Padding="14,12" BackgroundColor="Transparent" StrokeThickness="0">
    <HorizontalStackLayout Spacing="14">
        <Image HeightRequest="22" WidthRequest="22" />
        <Label FontSize="16" VerticalOptions="Center" />
    </HorizontalStackLayout>
</Border>

<!-- Good: define once in RidebaseStyles.xaml -->
<Style x:Key="MenuItemBorder" TargetType="Border">
    <Setter Property="Padding" Value="14,12" />
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="StrokeThickness" Value="0" />
</Style>

<!-- Then use it -->
<Border Style="{StaticResource MenuItemBorder}"> ... </Border>
```

## File Locations

| File | Purpose |
|---|---|
| `Resources/Styles/Colors.xaml` | All color definitions and brushes |
| `Resources/Styles/Styles.xaml` | Default MAUI implicit styles |
| `Resources/Styles/RidebaseStyles.xaml` | App-specific reusable styles |
