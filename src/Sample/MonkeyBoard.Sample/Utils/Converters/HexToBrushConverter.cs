using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Drawing;
using System.Globalization;
using Color = Avalonia.Media.Color;
namespace MonkeyBoard.Sample;

public class HexToBrushConverter : IValueConverter {
    public static HexToBrushConverter Instance { get; } = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if(value is not string hex) {
            return null;
        }
        System.Drawing.Color color = ColorTranslator.FromHtml(hex);
        return new SolidColorBrush(new Color(color.A, color.R, color.G, color.B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}