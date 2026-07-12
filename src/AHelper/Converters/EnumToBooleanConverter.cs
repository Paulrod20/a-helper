using System.Globalization;
using System.Windows.Data;

namespace AHelper.Converters;

/// <summary>
/// Converts an enum value to true/false by comparing it against a target value
/// passed in as the converter parameter. Lets a RadioButton's isChecked state
/// reflect whether it matches the currently selected enum value.
/// </summary>

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is true) ? Enum.Parse(targetType, parameter?.ToString() ?? string.Empty) : System.Windows.Data.Binding.DoNothing;
    }
}
