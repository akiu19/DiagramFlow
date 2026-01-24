using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace DiagramFlow.Behaviors
{
    public class ZoomBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty ZoomScaleProperty =
            DependencyProperty.Register("ZoomScale", typeof(double), typeof(ZoomBehavior),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double ZoomScale
        {
            get { return (double)GetValue(ZoomScaleProperty); }
            set { SetValue(ZoomScaleProperty, value); }
        }

        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ZoomBehavior));

        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (ScrollViewer != null)
            {
                ScrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            }
            // Support Left Double Click (Preview to ensure we catch it before children handle it)
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (ScrollViewer != null)
            {
                ScrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
            }
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;

                double oldScale = ZoomScale;
                double delta = e.Delta > 0 ? 1.2 : 0.8333;
                double newScale = Math.Max(0.1, Math.Min(4.0, oldScale * delta));

                ZoomToPoint(newScale, e.GetPosition(ScrollViewer), false);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                HandleDoubleClick(e);
            }
        }

        private void HandleDoubleClick(MouseButtonEventArgs e)
        {
             // Toggle zoom
            double targetScale = (Math.Abs(ZoomScale - 1.0) < 0.01) ? 2.0 : 1.0;
            // Use ScrollViewer position for center
            Point center = e.GetPosition(ScrollViewer);
            ZoomToPoint(targetScale, center, true);
            e.Handled = true;
        }
        
        private void ZoomToPoint(double targetScale, Point centerPoint, bool animate)
        {
            if (ScrollViewer == null) return;

            double startScale = ZoomScale;
            double startHorizontalOffset = ScrollViewer.HorizontalOffset;
            double startVerticalOffset = ScrollViewer.VerticalOffset;

            // Current content point under center
            Point contentPoint = new Point(
                (startHorizontalOffset + centerPoint.X) / startScale,
                (startVerticalOffset + centerPoint.Y) / startScale
            );

            // Calculate offsets for target scale to keep contentPoint at center
            // Viewport might change? Assuming similar viewport
            double targetHorizontalOffset = contentPoint.X * targetScale - (ScrollViewer.ViewportWidth / 2);
            double targetVerticalOffset = contentPoint.Y * targetScale - (ScrollViewer.ViewportHeight / 2);
            
            // Adjust bounds (min 0) handled by ScrollViewer usually, but for calculation:
             // targetHorizontalOffset = Math.Max(0, targetHorizontalOffset);
             // targetVerticalOffset = Math.Max(0, targetVerticalOffset);

            if (animate)
            {
                var animation = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var clock = animation.CreateClock();
                clock.CurrentTimeInvalidated += (s, e) =>
                {
                    if (ScrollViewer == null) 
                    {
                        clock.Controller.Stop();
                        return;
                    }
                    
                    double progress = clock.CurrentProgress ?? 0.0;
                    IEasingFunction easing = animation.EasingFunction;
                    double easedProgress = easing != null ? easing.Ease(progress) : progress;

                    double currentScale = startScale + (targetScale - startScale) * easedProgress;
                    double currentH = startHorizontalOffset + (targetHorizontalOffset - startHorizontalOffset) * easedProgress;
                    double currentV = startVerticalOffset + (targetVerticalOffset - startVerticalOffset) * easedProgress;

                    ZoomScale = currentScale;
                    ScrollViewer.UpdateLayout(); 
                    ScrollViewer.ScrollToHorizontalOffset(currentH);
                    ScrollViewer.ScrollToVerticalOffset(currentV);
                };
                clock.Controller.Begin();
            }
            else
            {
                ZoomScale = targetScale;
                ScrollViewer.UpdateLayout();
                ScrollViewer.ScrollToHorizontalOffset(targetHorizontalOffset);
                ScrollViewer.ScrollToVerticalOffset(targetVerticalOffset);
            }
        }
    }
}