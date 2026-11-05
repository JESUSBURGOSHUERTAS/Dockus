using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Dockus.Converters;

public class PercentToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double pct = 0;
        if (value is double d) pct = d;
        else if (value is float f) pct = f;
        else if (value is int i) pct = i;
        else if (value is bool b)
        {
            // For active sync dot
            return b 
                ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) // Emerald Green
                : new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Sky Blue
        }

        if (pct >= 90)
            return new SolidColorBrush(Color.FromRgb(244, 63, 94)); // Rose / Red
        if (pct >= 75)
            return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber / Yellow
        if (pct >= 50)
            return new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Sky Blue

        return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald Green
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool val && val;
        return b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
