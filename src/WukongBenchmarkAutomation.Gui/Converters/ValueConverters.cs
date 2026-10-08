using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WukongBenchmarkAutomation.Gui.ViewModels;

namespace WukongBenchmarkAutomation.Gui.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StepHighlightConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveGold = new((Color)ColorConverter.ConvertFromString("#E5A93C"));
    private static readonly SolidColorBrush PassedGreen = new((Color)ColorConverter.ConvertFromString("#2ECC71"));
    private static readonly SolidColorBrush InactiveGray = new((Color)ColorConverter.ConvertFromString("#333342"));
    private static readonly SolidColorBrush InactiveText = new((Color)ColorConverter.ConvertFromString("#777785"));
    private static readonly SolidColorBrush ActiveText = new((Color)ColorConverter.ConvertFromString("#F0F0F5"));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PipelineStep current && parameter is string targetStr && int.TryParse(targetStr, out var targetIndex))
        {
            var currentIndex = (int)current;
            var isForeground = targetType == typeof(Brush) && culture.Name == "foreground";

            if (currentIndex > targetIndex || current == PipelineStep.Completed)
            {
                return isForeground ? ActiveText : PassedGreen;
            }
            if (currentIndex == targetIndex)
            {
                return isForeground ? ActiveText : ActiveGold;
            }

            return isForeground ? InactiveText : InactiveGray;
        }

        return InactiveGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
