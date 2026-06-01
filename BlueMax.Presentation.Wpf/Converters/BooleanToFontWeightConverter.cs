using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueMax.Presentation.Wpf.Converters;

public class BooleanToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isBold && isBold)
        {
            return FontWeights.Bold;
        }
        return FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FontWeight weight && weight == FontWeights.Bold)
        {
            return true;
        }
        return false;
    }
}
