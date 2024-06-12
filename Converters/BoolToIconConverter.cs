using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using BootstrapIcons.Net;

namespace Ranger2.Converters
{
    public class BoolToIconConverter : DependencyObject, IValueConverter
    {
        public BootstrapIconGlyph FalseValue
        {
            get => (BootstrapIconGlyph)GetValue(FalseValueProperty);
            set => SetValue(FalseValueProperty, value);
        }

        public BootstrapIconGlyph TrueValue
        {
            get => (BootstrapIconGlyph)GetValue(TrueValueProperty);
            set => SetValue(TrueValueProperty, value);
        }

        public static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(nameof(FalseValue),
                                                                                                   typeof(BootstrapIconGlyph),
                                                                                                   typeof(BoolToIconConverter),
                                                                                                   new PropertyMetadata(null));


        public static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(nameof(TrueValue),
                                                                                                  typeof(BootstrapIconGlyph),
                                                                                                  typeof(BoolToIconConverter),
                                                                                                  new PropertyMetadata(null));

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
