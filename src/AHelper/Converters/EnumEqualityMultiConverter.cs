using System.Globalization;
using System.Windows.Data;

namespace AHelper.Converters;

/// <summary>
/// Compares two enum values passed in via MultiBinding and returns whether they're equal.
/// Exists because ConverterParameter can't hold a live Binding (WPF throws a XamlParseException
/// if you try), so comparing a per-item value against a shared "selected" value needs two
/// bound values instead of a value + a static parameter.
/// </summary>
public class EnumEqualityMultiConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] is null || values[1] is null)
            return false;

        return values[0]!.Equals(values[1]);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Selection changes flow through SelectModeCommand, not back through this binding.");
    }
}
