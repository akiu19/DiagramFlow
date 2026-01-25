using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using DiagramFlow.ViewModels;
using DiagramFlow.Helpers;

namespace DiagramFlow.Behaviors
{
    public class ConnectionCreationBehavior : Behavior<Canvas>
    {
        private bool _isConnecting;
        private NodeViewModel _sourceNode;
        private int _sourcePort;

        public static readonly DependencyProperty MainViewModelProperty =
            DependencyProperty.Register("MainViewModel", typeof(MainViewModel), typeof(ConnectionCreationBehavior));

        public MainViewModel MainViewModel
        {
            get { return (MainViewModel)GetValue(MainViewModelProperty); }
            set { SetValue(MainViewModelProperty, value); }
        }

        public static readonly DependencyProperty TempConnectionLineProperty =
            DependencyProperty.Register("TempConnectionLine", typeof(Line), typeof(ConnectionCreationBehavior));

        public Line TempConnectionLine
        {
            get { return (Line)GetValue(TempConnectionLineProperty); }
            set { SetValue(TempConnectionLineProperty, value); }
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
            if (e.Handled) return;

            // ポート（タグ付きのEllipse）上でクリックしたかどうかを確認
            if (e.OriginalSource is Ellipse port && port.Tag != null)
            {
                var node = VisualHelper.FindParentDataContext<NodeViewModel>(port);
                if (node == null) return;

                if (!int.TryParse(port.Tag.ToString(), out int portIndex)) return;

                _isConnecting = true;
                _sourceNode = node;
                _sourcePort = portIndex;

                var sourcePos = node.GetPortPosition(_sourcePort);

                if (TempConnectionLine != null)
                {
                    TempConnectionLine.X1 = sourcePos.X;
                    TempConnectionLine.Y1 = sourcePos.Y;
                    TempConnectionLine.X2 = sourcePos.X;
                    TempConnectionLine.Y2 = sourcePos.Y;
                    TempConnectionLine.Visibility = Visibility.Visible;
                }

                AssociatedObject.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isConnecting || TempConnectionLine == null) return;

            Point currentPos = e.GetPosition(AssociatedObject);
            TempConnectionLine.X2 = currentPos.X;
            TempConnectionLine.Y2 = currentPos.Y;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                AssociatedObject.ReleaseMouseCapture();

                if (TempConnectionLine != null)
                {
                    TempConnectionLine.Visibility = Visibility.Collapsed;
                }

                Point dropPoint = e.GetPosition(AssociatedObject);
                var (targetNode, targetPort) = FindNodeAndPortAtPoint(dropPoint);

                if (targetNode != null && MainViewModel != null)
                {
                    // コネクションを作成
                    var connector = new ConnectorViewModel(
                        _sourceNode, _sourcePort,
                        targetNode, targetPort);

                    MainViewModel.UndoService.Execute(
                        new DiagramFlow.Services.AddConnectionCommand(MainViewModel, connector));
                }

                _sourceNode = null;
                e.Handled = true;
            }
        }

        private (NodeViewModel node, int port) FindNodeAndPortAtPoint(Point point)
        {
            if (MainViewModel == null) return (null, 0);

            foreach (var node in MainViewModel.Nodes)
            {
                // ポイントがいずれかのポートの近くにあるかどうかを確認
                for (int port = 0; port < 4; port++)
                {
                    var portPos = node.GetPortPosition(port);
                    double distance = Math.Sqrt(
                        Math.Pow(point.X - portPos.X, 2) +
                        Math.Pow(point.Y - portPos.Y, 2));

                    if (distance < 15) // 15ピクセルの許容範囲
                    {
                        return (node, port);
                    }
                }

                // ポートの近くでない場合、ノード内かどうかを確認して最も近いポートを選択
                Rect nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
                if (nodeRect.Contains(point))
                {
                    // 最も近いポートを検索
                    int nearestPort = 0;
                    double minDistance = double.MaxValue;
                    for (int p = 0; p < 4; p++)
                    {
                        var portPos = node.GetPortPosition(p);
                        double dist = Math.Sqrt(
                            Math.Pow(point.X - portPos.X, 2) +
                            Math.Pow(point.Y - portPos.Y, 2));
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestPort = p;
                        }
                    }
                    return (node, nearestPort);
                }
            }

            return (null, 0);
        }
    }
}