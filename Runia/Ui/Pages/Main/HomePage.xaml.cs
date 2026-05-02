using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Runia.Ui.PagesMain
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private void NewsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            double step = 350;
            double target = sv.HorizontalOffset - (Math.Sign(e.Delta) * step);

            if (target < 0) target = 0;
            if (target > sv.ScrollableWidth) target = sv.ScrollableWidth;

            DoubleAnimation anim = new(sv.HorizontalOffset, target, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut }
            };

            sv.BeginAnimation(ScrollHelper.CurrentHorizontalOffsetProperty, null);
            sv.BeginAnimation(ScrollHelper.CurrentHorizontalOffsetProperty, anim);

            e.Handled = true;
        }

        public static class ScrollHelper
        {
            public static readonly DependencyProperty CurrentHorizontalOffsetProperty =
                DependencyProperty.RegisterAttached("CurrentHorizontalOffset", typeof(double), typeof(ScrollHelper),
                    new PropertyMetadata(0.0, (d, e) => ((ScrollViewer)d).ScrollToHorizontalOffset((double)e.NewValue)));
            public static void SetCurrentHorizontalOffset(DependencyObject d, double v) => d.SetValue(CurrentHorizontalOffsetProperty, v);
            public static double GetCurrentHorizontalOffset(DependencyObject d) => (double)d.GetValue(CurrentHorizontalOffsetProperty);
        }
    }
}