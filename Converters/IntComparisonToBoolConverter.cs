using System;
using System.Globalization;
using System.Windows.Data;

namespace Ranger2.Converters
{
    public class IntComparisonToBoolConverter : IValueConverter
    {
        private enum Op
        {
            LessThan,
            LessThanOrEqual,
            Equal,
            GreaterThanOrEqual,
            GreaterThan,
            NotEqual
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertInternal(value, parameter);
        }

        protected bool ConvertInternal(object value, object parameter)
        {
            bool success = false;

            int intValue = System.Convert.ToInt32(value);

            if (parameter is string args)
            {
                if (args.StartsWith("="))
                {
                    if (int.TryParse(args.Substring(1), out int numberToCompare))
                    {
                        success = (intValue == numberToCompare);
                    }
                }
                else if (args.StartsWith("!="))
                {
                    if (int.TryParse(args.Substring(2), out int numberToCompare))
                    {
                        success = (intValue != numberToCompare);
                    }
                }
                else if (args.StartsWith("<="))
                {
                    if (int.TryParse(args.Substring(2), out int numberToCompare))
                    {
                        success = (intValue <= numberToCompare);
                    }
                }
                else if (args.StartsWith(">="))
                {
                    if (int.TryParse(args.Substring(2), out int numberToCompare))
                    {
                        success = (intValue >= numberToCompare);
                    }
                }
                else if (args.StartsWith("<"))
                {
                    if (int.TryParse(args.Substring(1), out int numberToCompare))
                    {
                        success = (intValue < numberToCompare);
                    }
                }
                else if (args.StartsWith(">"))
                {
                    if (int.TryParse(args.Substring(1), out int numberToCompare))
                    {
                        success = (intValue > numberToCompare);
                    }
                }
                else
                {
                    // Assume equals by default
                    if (int.TryParse(args, out int numberToCompare))
                    {
                        success = (intValue == numberToCompare);
                    }
                }
            }

            return success;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(parameter.ToString(), out int valueAsInt))
            {
                return valueAsInt;
            }

            return 0;
        }
    }
}
