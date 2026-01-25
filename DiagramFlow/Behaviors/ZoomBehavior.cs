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
            // 左ダブルクリックをサポート（子要素より先にキャッチするためにPreviewを使用）
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
                // アイテム（ノードまたはコネクター）上でクリックした場合は無視
                if (e.OriginalSource is DependencyObject obj)
                {
                    if (IsItem(obj)) return;
                }
                
                HandleDoubleClick(e);
            }
        }

        private bool IsItem(DependencyObject obj)
        {
            // ノードまたはコネクター内にいるかどうかを確認するため上位を走査
            // DataContextを確認
            if (obj is FrameworkElement fe)
            {
                if (fe.DataContext != null && 
                    (fe.DataContext.GetType().Name.Contains("NodeViewModel") || 
                     fe.DataContext.GetType().Name.Contains("ConnectorViewModel")))
                {
                    return true;
                }
            }
            
            // または単純なチェック: 関連オブジェクトまたはその直接の背景でない場合
            // ただし、AssociatedObject は Grid コンテナ。
            // アイテムは子要素。
            // DataContext チェックは堅牢。
            
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent != null)
                return IsItem(parent);
                
            return false;
        }

        private void HandleDoubleClick(MouseButtonEventArgs e)
        {
             // ズームを切り替え
            double targetScale = (Math.Abs(ZoomScale - 1.0) < 0.01) ? 2.0 : 1.0;
            // 中心の ScrollViewer 位置を使用
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

            // 中心下の現在のコンテンツポイント
            Point contentPoint = new Point(
                (startHorizontalOffset + centerPoint.X) / startScale,
                (startVerticalOffset + centerPoint.Y) / startScale
            );

            // contentPoint を中心に維持するためのターゲットスケールのオフセットを計算
            // ビューポートが変更される可能性がある？類似のビューポートを想定
            double targetHorizontalOffset = contentPoint.X * targetScale - (ScrollViewer.ViewportWidth / 2);
            double targetVerticalOffset = contentPoint.Y * targetScale - (ScrollViewer.ViewportHeight / 2);
            
            // 境界を調整（最小 0）は通常 ScrollViewer で処理されるが、計算のため:
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