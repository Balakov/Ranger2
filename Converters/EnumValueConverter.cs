using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ranger2.Converters
{
    public class EnumerationValueConverterBase
    {
        public bool Invert { get; set; }

        public bool ConvertInternal(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = false;
            string valueString = value?.ToString();
            string comparisionString = parameter?.ToString();

            if (value.GetType().IsEnum &&
                !string.IsNullOrEmpty(valueString) &&
                !string.IsNullOrEmpty(comparisionString))
            {
                b = value.ToString() == parameter.ToString();

                if (Invert)
                {
                    b = !b;
                }
            }

            return b;
        }
    }

    public class EnumerationValueToBooleanConverter : EnumerationValueConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertInternal(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }

    public class EnumerationValueToVisibilityConverter : EnumerationValueConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertInternal(value, targetType, parameter, culture) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
