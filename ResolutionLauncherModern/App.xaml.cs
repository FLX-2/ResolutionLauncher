using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ResolutionLauncherModern
{
    public partial class App : Application
    {
    }

    public class BrushLightenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color color = brush.Color;
                return new SolidColorBrush(Color.FromArgb(color.A, (byte)(color.R * 1.2), (byte)(color.G * 1.2), (byte)(color.B * 1.2)));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BrushDarkenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color color = brush.Color;
                return new SolidColorBrush(Color.FromArgb(color.A, (byte)(color.R * 0.8), (byte)(color.G * 0.8), (byte)(color.B * 0.8)));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
