using System;
using System.Globalization;
using System.Windows.Data;
using System.Data;

namespace DiagramFlow.Converters
{
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string expression = parameter as string;
                if (string.IsNullOrEmpty(expression)) return null;

                double x = System.Convert.ToDouble(value);
                expression = expression.Replace("x", x.ToString(CultureInfo.InvariantCulture));

                // Using DataTable.Compute for simple math evaluation
                var result = new DataTable().Compute(expression, null);
                return System.Convert.ToDouble(result);
            }
            catch
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
