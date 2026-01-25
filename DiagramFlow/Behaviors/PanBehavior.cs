using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace DiagramFlow.Behaviors
{
    public class PanBehavior : Behavior<ScrollViewer>
    {
        private Point _lastPanPosition;
        private bool _isPanning;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseRightButtonDown += AssociatedObject_PreviewMouseRightButtonDown;
            AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
            AssociatedObject.PreviewMouseRightButtonUp += AssociatedObject_PreviewMouseRightButtonUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseRightButtonDown -= AssociatedObject_PreviewMouseRightButtonDown;
            AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
            AssociatedObject.PreviewMouseRightButtonUp -= AssociatedObject_PreviewMouseRightButtonUp;
        }

        private void AssociatedObject_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                return;
            }

            _lastPanPosition = e.GetPosition(AssociatedObject);
            _isPanning = true;
            AssociatedObject.CaptureMouse();
            AssociatedObject.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPos = e.GetPosition(AssociatedObject);
                Vector delta = currentPos - _lastPanPosition;

                AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.HorizontalOffset - delta.X);
                AssociatedObject.ScrollToVerticalOffset(AssociatedObject.VerticalOffset - delta.Y);

                _lastPanPosition = currentPos;
            }
        }

        private void AssociatedObject_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                AssociatedObject.ReleaseMouseCapture();
                AssociatedObject.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }
    }
}