using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace KafkaLoad.UI.Helpers;

public class BytesToMegabytesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double bytes)
        {
            return bytes / (1024 * 1024);
        }
        if (value is long longBytes)
        {
            return (double)longBytes / (1024 * 1024);
        }
        if (value is int intBytes)
        {
            return (double)intBytes / (1024 * 1024);
        }

        return 0d;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
