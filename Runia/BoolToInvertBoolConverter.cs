using System;
using System.Globalization;
using System.Windows.Data;

namespace Runia
{
    /// <summary>
    /// Конвертирует булевое значение в противоположное булево значение
    /// Используется для инвертирования логики привязок (напр. IsEnabled={Binding IsBusy, Converter=...})
    /// </summary>
    public class BoolToInvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
