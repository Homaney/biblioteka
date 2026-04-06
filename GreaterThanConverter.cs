using System;
using System.Globalization;
using System.Windows.Data;

namespace biblioteka
{
    public class GreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramString)
            {
                if (int.TryParse(paramString, out int compareValue))
                {
                    return intValue > compareValue;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}