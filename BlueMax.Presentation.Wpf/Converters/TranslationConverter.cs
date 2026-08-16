using System;
using System.Globalization;
using System.Windows.Data;
using BlueMax.Presentation.Wpf.Resources;

namespace BlueMax.Presentation.Wpf.Converters;

public class TranslationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            return Translations.Get(key);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
