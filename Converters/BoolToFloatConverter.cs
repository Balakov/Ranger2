using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Ranger2.Converters
{
    public class BoolToFloatConverter : DependencyObject, IValueConverter
    {
        public float FalseValue { get; set; }
        public float TrueValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                return TrueValue;
            }

            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
