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
        private AnimationClock _animationClock;
        private EventHandler _animationTickHandler;
        
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
            
            // Stop any ongoing animation to prevent null reference exceptions
            StopAnimation();
            
            if (ScrollViewer != null)
            {
                ScrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
            }
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
        }
        
        private void StopAnimation()
        {
            if (_animationClock != null)
            {
                if (_animationTickHandler != null)
                {
                    _animationClock.CurrentTimeInvalidated -= _animationTickHandler;
                    _animationTickHandler = null;
                }
                
                if (_animationClock.Controller != null)
                {
                    _animationClock.Controller.Stop();
                }
                
                _animationClock = null;
            }
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
                // Ignore if clicked on an item (Node or Connector)
                if (e.OriginalSource is DependencyObject obj)
                {
                    if (IsItem(obj)) return;
                }
                
                HandleDoubleClick(e);
            }
        }

        private bool IsItem(DependencyObject obj)
        {
            // Traverse up to find if we are in a Node or Connector
            // Check DataContext
            if (obj is FrameworkElement fe)
            {
                if (fe.DataContext != null && 
                    (fe.DataContext.GetType().Name.Contains("NodeViewModel") || 
                     fe.DataContext.GetType().Name.Contains("ConnectorViewModel")))
                {
                    return true;
                }
            }
            
            // Or simple check: if it is not the associated object or its direct background
            // But AssociatedObject is the Grid container.
            // Items are children.
            // DataContext check is robust.
            
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent != null)
                return IsItem(parent);
                
            return false;
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

            // Stop any existing animation before starting a new one
            StopAnimation();

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

                _animationClock = animation.CreateClock();
                
                // Use local variables for closure to avoid issues with changing state
                var localStartScale = startScale;
                var localStartH = startHorizontalOffset;
                var localStartV = startVerticalOffset;
                var localTargetScale = targetScale;
                var localTargetH = targetHorizontalOffset;
                var localTargetV = targetVerticalOffset;
                var localEasing = animation.EasingFunction;
                
                _animationTickHandler = (s, e) =>
                {
                    // Verify ScrollViewer is still valid and animation clock exists
                    if (ScrollViewer == null || _animationClock == null || _animationClock.Controller == null)
                    {
                        StopAnimation();
                        return;
                    }
                    
                    // Check if animation is still active
                    if (_animationClock.CurrentState == ClockState.Stopped)
                    {
                        StopAnimation();
                        return;
                    }
                    
                    double progress = _animationClock.CurrentProgress ?? 0.0;
                    double easedProgress = localEasing != null ? localEasing.Ease(progress) : progress;

                    double currentScale = localStartScale + (localTargetScale - localStartScale) * easedProgress;
                    double currentH = localStartH + (localTargetH - localStartH) * easedProgress;
                    double currentV = localStartV + (localTargetV - localStartV) * easedProgress;

                    try
                    {
                        ZoomScale = currentScale;
                        ScrollViewer.UpdateLayout(); 
                        ScrollViewer.ScrollToHorizontalOffset(currentH);
                        ScrollViewer.ScrollToVerticalOffset(currentV);
                    }
                    catch (Exception)
                    {
                        // If any operation fails (e.g., due to detachment), stop the animation
                        StopAnimation();
                    }
                };
                
                _animationClock.CurrentTimeInvalidated += _animationTickHandler;
                _animationClock.Controller.Begin();
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