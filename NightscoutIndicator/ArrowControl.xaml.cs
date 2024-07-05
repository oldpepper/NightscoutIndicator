using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NightscoutIndicator
{
    /// <summary>
    /// Trend arrow control.
    /// </summary>
    public partial class ArrowControl : UserControl
    {
        public ArrowControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ArrowColorProperty =
            DependencyProperty.Register(nameof(ArrowColor), typeof(Brush), typeof(ArrowControl),
                                        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(204, 200, 200, 200)))); // 50% transparency

        public Brush ArrowColor
        {
            get => (Brush)GetValue(ArrowColorProperty);
            set => SetValue(ArrowColorProperty, value);
        }

        public static readonly DependencyProperty GlucoseColorProperty =
            DependencyProperty.Register(nameof(GlucoseColor), typeof(Brush), typeof(ArrowControl),
                                        new PropertyMetadata(Brushes.Orchid));

        public Brush GlucoseColor
        {
            get => (Brush)GetValue(GlucoseColorProperty);
            set => SetValue(GlucoseColorProperty, value);
        }

        // Background size property
        public static readonly DependencyProperty IndicatorSizeProperty =
            DependencyProperty.Register(nameof(IndicatorSize), typeof(double), typeof(ArrowControl),
                                        new PropertyMetadata(50.0));

        public double IndicatorSize
        {
            get => (double)GetValue(IndicatorSizeProperty);
            set => SetValue(IndicatorSizeProperty, value);
        }
    }
}
