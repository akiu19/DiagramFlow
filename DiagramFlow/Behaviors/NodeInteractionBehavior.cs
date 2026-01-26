using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using DiagramFlow.ViewModels;
using DiagramFlow.Helpers;

namespace DiagramFlow.Behaviors
{
    public class NodeInteractionBehavior : Behavior<FrameworkElement>
    {
        private Point _dragStartPosition;
        private bool _isDragging;
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastMousePosition;
        private Point _dragStartCanvasPosition;

        public static readonly DependencyProperty MainViewModelProperty =
            DependencyProperty.Register("MainViewModel", typeof(MainViewModel), typeof(NodeInteractionBehavior));

        public MainViewModel MainViewModel
        {
            get { return (MainViewModel)GetValue(MainViewModelProperty); }
            set { SetValue(MainViewModelProperty, value); }
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
            var node = AssociatedObject.DataContext as NodeViewModel;
            if (node == null || MainViewModel == null) return;

            // ダブルクリックのチェック
            if (e.ClickCount == 2)
            {
                node.IsEditing = true;
                e.Handled = true;
                return;
            }

            // 選択ロジック
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                MainViewModel.ToggleNodeSelection(node);
            }
            else
            {
                if (!node.IsSelected)
                {
                    MainViewModel.SelectNode(node);
                }
            }

            // ドラッグを開始
            _isDragging = true;
            _dragStartPosition = e.GetPosition(AssociatedObject);
            
            // 祖先Canvasを探す:
            var canvas = VisualHelper.FindParent<Canvas>(AssociatedObject);
            if (canvas != null)
            {
                _lastMousePosition = e.GetPosition(canvas);
                _dragStartCanvasPosition = _lastMousePosition;
            }
            
            AssociatedObject.CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || MainViewModel == null) return;

            var canvas = VisualHelper.FindParent<Canvas>(AssociatedObject);
            if (canvas == null) return;

            Point currentPos = e.GetPosition(canvas);
            Vector delta = currentPos - _lastMousePosition;

            if (delta.Length > 0)
            {
                MainViewModel.MoveSelectedNodes(delta.X, delta.Y);
                _lastMousePosition = currentPos;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                AssociatedObject.ReleaseMouseCapture();
                
                var canvas = VisualHelper.FindParent<Canvas>(AssociatedObject);
                if (canvas != null && MainViewModel != null)
                {
                    Point currentPos = e.GetPosition(canvas);
                    Vector totalDelta = currentPos - _dragStartCanvasPosition;

                    if (totalDelta.Length > 0.1)
                    {
                         MainViewModel.UndoService.AddToHistory(
                             new DiagramFlow.Services.MoveNodesCommand(
                                 MainViewModel.SelectedNodes, totalDelta.X, totalDelta.Y));
                    }
                }
                
                e.Handled = true;
            }
        }
    }
}