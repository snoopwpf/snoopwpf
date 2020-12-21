namespace Snoop.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    public class BrushStop
    {
        public BrushStop(Color color, double offset)
        {
            this.Color = color;
            this.Offset = offset;
            this.ColorText = this.Color.ToString();
        }

        public Color Color { get; }

        public double Offset { get; }

        public string ColorText { get; }
    }

    public class BrushStopsConverter : IValueConverter
    {
        public static readonly BrushStopsConverter DefaultInstance = new();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BuildStops((Brush)value);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static List<BrushStop> BuildStops(Brush brush)
        {
            var stops = new List<BrushStop>();

            if (brush is SolidColorBrush solidColorBrush)
            {
                stops.Add(new BrushStop(solidColorBrush.Color, 0));
            }
            else if (brush is GradientBrush gradientBrush)
            {
                foreach (var gradientStop in gradientBrush.GradientStops)
                {
                    stops.Add(new BrushStop(gradientStop.Color, gradientStop.Offset));
                }
            }

            return stops;
        }
    }
}