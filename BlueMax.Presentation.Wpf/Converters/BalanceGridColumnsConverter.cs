using System;
using System.Globalization;
using System.Windows.Data;

namespace BlueMax.Presentation.Wpf.Converters;

/// <summary>
/// Computes the number of columns for a UniformGrid so that the item count
/// divides evenly (no orphaned last row), based on the available width.
/// </summary>
public sealed class BalanceGridColumnsConverter : IMultiValueConverter
{
    public double MinCardWidth { get; set; } = 360;
    public int MaxColumns { get; set; } = 6;
    public int MinColumns { get; set; } = 1;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double width = values.Length > 0 && values[0] is double w ? w : 0d;
        int count = values.Length > 1 && values[1] is int n ? n : 0;

        if (width <= 0)
            return MinColumns;

        int maxColumns = (int)Math.Floor(width / Math.Max(1d, MinCardWidth));
        maxColumns = Math.Max(MinColumns, Math.Min(maxColumns, MaxColumns));

        if (count > 0)
        {
            for (int c = maxColumns; c >= MinColumns; c--)
            {
                if (count % c == 0)
                    return c;
            }
        }

        return maxColumns;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
