using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BlueMax.Presentation.Wpf.Converters;

public class BooleanToTextDecorationsConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUnderline && isUnderline)
        {
            return TextDecorations.Underline;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TextDecorationCollection decorations && decorations == TextDecorations.Underline)
        {
            return true;
        }
        return false;
    }
}
