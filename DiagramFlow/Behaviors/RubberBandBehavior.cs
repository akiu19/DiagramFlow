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
            // 別の操作がアクティブ（キャプチャ済み）の場合は開始しない。
            // または e.Source が Canvas でない場合（例: アイテム上でクリックした場合）は開始しない。
            // ただし、アイテムは通常独自のイベントを処理する。
            // e.Handled が true の場合は何もしない。
            if (e.Handled) return;
            
            // アイテム上でクリックしたかどうかを確認する必要があるか？
            // 厳密な `e.OriginalSource == AssociatedObject` チェックを使用すると、背景のグリッド線などをクリックした場合に失敗する可能性がある。
            // ただし、一般的にCanvas背景の場合、OriginalSource は Canvas または Grid になる。
            
            // MainWindow のロジック: if (_currentState != OperationState.Idle) return;
            // Behavior はグローバル状態を簡単に知ることができない。
            // ただし、通常 MouseCapture は他のイベントを防ぐ。

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

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || SelectionRectangle == null) return;

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

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                AssociatedObject.ReleaseMouseCapture();

                if (SelectionRectangle != null)
                {
                    SelectionRectangle.Visibility = Visibility.Collapsed;
                    ProcessSelection();
                }

                e.Handled = false; // 他に許可する？いいえ、選択アクションを処理した。
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
    }
}