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

        public Color Color { get; private set; }

        public double Offset { get; private set; }

        public string ColorText { get; private set; }
    }

    public class BrushStopsConverter : IValueConverter
    {
        public static BrushStopsConverter DefaultInstance = new BrushStopsConverter();

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

            var solidColorBrush = brush as SolidColorBrush;
            if (solidColorBrush != null)
            {
                stops.Add(new BrushStop(solidColorBrush.Color, 0));
            }

            var gradientBrush = brush as GradientBrush;
            if (gradientBrush != null)
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