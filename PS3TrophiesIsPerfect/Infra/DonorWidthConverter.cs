using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PS3TrophiesIsPerfect.Infra
{
    /// <summary>true → a star-sized donor column; false → 0 width (panel collapsed).</summary>
    public sealed class DonorWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? new GridLength(1.25, GridUnitType.Star) : new GridLength(0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}
