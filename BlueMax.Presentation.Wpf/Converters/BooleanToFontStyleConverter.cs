using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueMax.Presentation.Wpf.Converters;

public class BooleanToFontStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isItalic && isItalic)
        {
            return FontStyles.Italic;
        }
        return FontStyles.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FontStyle style && style == FontStyles.Italic)
        {
            return true;
        }
        return false;
    }
}
