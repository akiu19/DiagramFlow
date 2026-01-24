using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Xaml.Behaviors;
using DiagramFlow.ViewModels;

namespace DiagramFlow.Behaviors
{
    public class RubberBandBehavior : Behavior<Canvas>
    {
        private Point _startPoint;
        private bool _isSelecting;

        public static readonly DependencyProperty MainViewModelProperty =
            DependencyProperty.Register("MainViewModel", typeof(MainViewModel), typeof(RubberBandBehavior));

        public MainViewModel MainViewModel
        {
            get { return (MainViewModel)GetValue(MainViewModelProperty); }
            set { SetValue(MainViewModelProperty, value); }
        }

        public static readonly DependencyProperty SelectionRectangleProperty =
            DependencyProperty.Register("SelectionRectangle", typeof(FrameworkElement), typeof(RubberBandBehavior));

        public FrameworkElement SelectionRectangle
        {
            get { return (FrameworkElement)GetValue(SelectionRectangleProperty); }
            set { SetValue(SelectionRectangleProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If another operation is active (captured), don't start.
            // Or if e.Source is not Canvas (e.g. clicked on an item), don't start.
            // But Items usually handle their own events.
            // If e.Handled is true, do nothing.
            if (e.Handled) return;
            
            // Should check if we are clicking on an item? 
            // If the strict `e.OriginalSource == AssociatedObject` check is used, it might fail if we click on background grid lines etc.
            // But generally for Canvas background, OriginalSource is the Canvas or Grid.
            
            // MainWindow logic was: if (_currentState != OperationState.Idle) return;
            // Behavior doesn't know about global state easily. 
            // But usually MouseCapture prevents other events.

            try
            {
                MainViewModel?.ClearSelection();

                _isSelecting = true;
                _startPoint = e.GetPosition(AssociatedObject);

                if (SelectionRectangle != null)
                {
                    SelectionRectangle.Visibility = Visibility.Visible;
                    SelectionRectangle.Width = 0;
                    SelectionRectangle.Height = 0;
                    Canvas.SetLeft(SelectionRectangle, _startPoint.X);
                    Canvas.SetTop(SelectionRectangle, _startPoint.Y);
                }

                AssociatedObject.CaptureMouse();
                e.Handled = true;
            }
            catch
            {
                // If an exception occurs, ensure we clean up properly
                CleanupSelection();
                throw;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || SelectionRectangle == null) return;

            try
            {
                Point currentPos = e.GetPosition(AssociatedObject);
                double x = Math.Min(_startPoint.X, currentPos.X);
                double y = Math.Min(_startPoint.Y, currentPos.Y);
                double width = Math.Abs(currentPos.X - _startPoint.X);
                double height = Math.Abs(currentPos.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }
            catch
            {
                // If an exception occurs during mouse move, clean up and stop selecting
                CleanupSelection();
                throw;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                try
                {
                    if (SelectionRectangle != null)
                    {
                        ProcessSelection();
                    }

                    e.Handled = true;
                }
                finally
                {
                    // Always clean up, even if an exception occurs
                    CleanupSelection();
                }
            }
        }

        private void ProcessSelection()
        {
            if (MainViewModel == null || SelectionRectangle == null) return;

            double x = Canvas.GetLeft(SelectionRectangle);
            double y = Canvas.GetTop(SelectionRectangle);
            
            Rect selectionRect = new Rect(x, y, SelectionRectangle.Width, SelectionRectangle.Height);

            bool addToSelection = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (!addToSelection)
            {
                MainViewModel.ClearSelection();
            }

            foreach (var node in MainViewModel.Nodes)
            {
                Rect nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
                if (selectionRect.IntersectsWith(nodeRect))
                {
                    if (!MainViewModel.SelectedNodes.Contains(node))
                    {
                        MainViewModel.SelectNode(node, true);
                    }
                }
            }
        }

        private void CleanupSelection()
        {
            _isSelecting = false;
            
            if (AssociatedObject != null && AssociatedObject.IsMouseCaptured)
            {
                AssociatedObject.ReleaseMouseCapture();
            }

            if (SelectionRectangle != null)
            {
                SelectionRectangle.Visibility = Visibility.Collapsed;
            }
        }
    }
}