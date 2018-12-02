using System.Windows.Media;

namespace Chat.ColorScheme
{
    public static class Palette
    {
        public static class Colors
        {
            public static Color BLACK_PEARL { get { return Color.FromRgb(30, 39, 46); } }
            public static Color CONCRETE { get { return Color.FromRgb(149, 165, 166); } }
            public static Color PIXELATED_GRASS { get { return Color.FromRgb(0, 148, 50); } }
            public static Color HARLEY_DAVIDSON_ORANGE { get { return Color.FromRgb(194, 54, 22); } }
            public static Color WHITE { get { return Color.FromRgb(255, 255, 255); } }
            public static Color SWAN_WHITE { get { return Color.FromRgb(247, 241, 227); } }
            public static Color PROTOSS_PYLON { get { return Color.FromRgb(0, 168, 255); } }
            public static Color BELIZE_HOLE { get { return Color.FromRgb(41, 128, 185); } }

        }
        public static class Brushes
        {
            public static SolidColorBrush WHITE { get { return new SolidColorBrush(Colors.WHITE); } }
            public static SolidColorBrush TRANS { get { return new SolidColorBrush(System.Windows.Media.Colors.Transparent); } }
            public static SolidColorBrush SWAN_WHITE { get { return new SolidColorBrush(Colors.SWAN_WHITE); } }
            public static SolidColorBrush PROTOSS_PYLON { get { return new SolidColorBrush(Colors.PROTOSS_PYLON); } }

        }
    }
}
