using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DiagramFlow.ViewModels;
using Microsoft.Win32;

namespace DiagramFlow
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Operation states
        private enum OperationState
        {
            Idle,
            Selecting,      // Rubber band selection
            DraggingNode,   // Moving node(s)
            ResizingNode,   // Resizing node
            Panning,        // Canvas pan
            Connecting      // Creating connection
        }

        private OperationState _currentState = OperationState.Idle;

        // Node drag operation
        private Point _dragStartPosition;
        private Dictionary<NodeViewModel, Point> _dragStartNodePositions = new Dictionary<NodeViewModel, Point>();
        private NodeViewModel _draggingNode;

        // Double click detection
        private DateTime _lastClickTime = DateTime.MinValue;
        private NodeViewModel _lastClickedNode;

        // Resize operation
        private Point _resizeStartSize;
        private NodeViewModel _resizingNode;

        // Rubber band selection
        private Point _selectionStartPoint;

        // Connection creation
        private NodeViewModel _connectionSourceNode;
        private int _connectionSourcePort;

        public MainWindow()
        {
            InitializeComponent();
        }

        private MainViewModel ViewModel => DataContext as MainViewModel;

        #region Zoom Operations

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ZoomScale = 1.0;
            }
        }

        #endregion

        #region Pan Operations

        // Pan operations moved to PanBehavior

        #endregion

        #region Canvas Operations

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentState != OperationState.Idle) return;

            // Start rubber band selection
            _selectionStartPoint = e.GetPosition(DiagramCanvas);
            _currentState = OperationState.Selecting;

            Canvas.SetLeft(SelectionRectangle, _selectionStartPoint.X);
            Canvas.SetTop(SelectionRectangle, _selectionStartPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;

            DiagramCanvas.CaptureMouse();

            // Clear selection if not holding Ctrl
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                ViewModel?.ClearSelection();
            }

            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = e.GetPosition(DiagramCanvas);

            switch (_currentState)
            {
                case OperationState.Selecting:
                    UpdateSelectionRectangle(currentPos);
                    break;

                case OperationState.Connecting:
                    TempConnectionLine.X2 = currentPos.X;
                    TempConnectionLine.Y2 = currentPos.Y;
                    break;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point currentPos = e.GetPosition(DiagramCanvas);

            switch (_currentState)
            {
                case OperationState.Selecting:
                    FinishRubberBandSelection();
                    break;

                case OperationState.Connecting:
                    // Check if dropped on a port
                    FinishConnection(currentPos);
                    break;
            }

            _currentState = OperationState.Idle;
            DiagramCanvas.ReleaseMouseCapture();
        }

        private void UpdateSelectionRectangle(Point currentPos)
        {
            double x = Math.Min(_selectionStartPoint.X, currentPos.X);
            double y = Math.Min(_selectionStartPoint.Y, currentPos.Y);
            double width = Math.Abs(currentPos.X - _selectionStartPoint.X);
            double height = Math.Abs(currentPos.Y - _selectionStartPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }

        private void FinishRubberBandSelection()
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;

            if (ViewModel == null) return;

            Rect selectionRect = new Rect(
                Canvas.GetLeft(SelectionRectangle),
                Canvas.GetTop(SelectionRectangle),
                SelectionRectangle.Width,
                SelectionRectangle.Height);

            // Select all nodes that intersect with the selection rectangle
            foreach (var node in ViewModel.Nodes)
            {
                Rect nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
                if (selectionRect.IntersectsWith(nodeRect))
                {
                    ViewModel.SelectNode(node, true);
                }
            }

            UpdateStatus($"Selected {ViewModel.SelectedNodes.Count} node(s)");
        }

        #endregion

        #region Node Operations

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentState != OperationState.Idle) return;

            var border = sender as Border;
            var node = border?.DataContext as NodeViewModel;
            if (node == null) return;

            // Double click detection
            DateTime now = DateTime.Now;
            if (_lastClickedNode == node && (now - _lastClickTime).TotalMilliseconds < 300)
            {
                // Double click detected - enter edit mode
                node.IsEditing = true;
                var textBox = FindVisualChild<TextBox>(border);
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
                _lastClickTime = DateTime.MinValue;
                e.Handled = true;
                return;
            }
            _lastClickTime = now;
            _lastClickedNode = node;

            // Handle selection
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ViewModel.ToggleNodeSelection(node);
            }
            else
            {
                if (!node.IsSelected)
                {
                    ViewModel.SelectNode(node);
                }
            }

            // Start dragging
            _currentState = OperationState.DraggingNode;
            _dragStartPosition = e.GetPosition(DiagramCanvas);
            _draggingNode = node;

            // Store start positions for all selected nodes
            _dragStartNodePositions.Clear();
            foreach (var selectedNode in ViewModel.SelectedNodes)
            {
                _dragStartNodePositions[selectedNode] = new Point(selectedNode.X, selectedNode.Y);
            }

            border.CaptureMouse();
            e.Handled = true;
        }

        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentState != OperationState.DraggingNode) return;

            Point currentPos = e.GetPosition(DiagramCanvas);
            Vector delta = currentPos - _dragStartPosition;

            // Move all selected nodes
            foreach (var kvp in _dragStartNodePositions)
            {
                kvp.Key.X = Math.Max(0, kvp.Value.X + delta.X);
                kvp.Key.Y = Math.Max(0, kvp.Value.Y + delta.Y);
            }
        }

        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentState == OperationState.DraggingNode)
            {
                _currentState = OperationState.Idle;
                var border = sender as Border;
                border?.ReleaseMouseCapture();

                // TODO: Create undo command for move operation

                _draggingNode = null;
                _dragStartNodePositions.Clear();
                e.Handled = true;
            }
        }

        private void NodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            var node = textBox?.DataContext as NodeViewModel;
            if (node == null) return;

            if (e.Key == Key.Enter)
            {
                node.IsEditing = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // TODO: Revert text change
                node.IsEditing = false;
                e.Handled = true;
            }
        }

        private void NodeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var node = textBox?.DataContext as NodeViewModel;
            if (node != null)
            {
                node.IsEditing = false;
            }
        }

        #endregion

        private void Connector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentState != OperationState.Idle) return;

            var line = sender as Line;
            var connector = line?.DataContext as ConnectorViewModel;
            if (connector == null || ViewModel == null) return;

            // Handle selection
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ViewModel.SelectedConnectors.Contains(connector))
                {
                    connector.IsSelected = false;
                    ViewModel.SelectedConnectors.Remove(connector);
                }
                else
                {
                    ViewModel.SelectConnector(connector, true);
                }
            }
            else
            {
                ViewModel.SelectConnector(connector);
            }

            e.Handled = true;
        }

        #region Resize Operations

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            var thumb = sender as Thumb;
            var node = FindParentDataContext<NodeViewModel>(thumb);
            if (node == null) return;

            _currentState = OperationState.ResizingNode;
            _resizingNode = node;
            _resizeStartSize = new Point(node.Width, node.Height);
            e.Handled = true;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_resizingNode == null) return;

            double newWidth = Math.Max(NodeViewModel.MinWidth, _resizingNode.Width + e.HorizontalChange);
            double newHeight = Math.Max(NodeViewModel.MinHeight, _resizingNode.Height + e.VerticalChange);

            _resizingNode.Width = newWidth;
            _resizingNode.Height = newHeight;

            e.Handled = true;
        }

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // TODO: Create undo command for resize operation
            _currentState = OperationState.Idle;
            _resizingNode = null;
            e.Handled = true;
        }

        #endregion

        #region Connection Operations

        private void Port_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentState != OperationState.Idle) return;

            var port = sender as Ellipse;
            var node = FindParentDataContext<NodeViewModel>(port);
            if (node == null || port?.Tag == null) return;

            _connectionSourceNode = node;
            _connectionSourcePort = int.Parse(port.Tag.ToString());

            var sourcePos = node.GetPortPosition(_connectionSourcePort);

            _currentState = OperationState.Connecting;
            TempConnectionLine.X1 = sourcePos.X;
            TempConnectionLine.Y1 = sourcePos.Y;
            TempConnectionLine.X2 = sourcePos.X;
            TempConnectionLine.Y2 = sourcePos.Y;
            TempConnectionLine.Visibility = Visibility.Visible;

            DiagramCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void FinishConnection(Point dropPoint)
        {
            TempConnectionLine.Visibility = Visibility.Collapsed;

            if (_connectionSourceNode == null || ViewModel == null) return;

            // Find target node and port under drop point
            var (targetNode, targetPort) = FindNodeAndPortAtPoint(dropPoint);

            if (targetNode != null)
            {
                // Create connection
                var connector = new ConnectorViewModel(
                    _connectionSourceNode, _connectionSourcePort,
                    targetNode, targetPort);

                ViewModel.Connectors.Add(connector);
                UpdateStatus("Connection created");
            }

            _connectionSourceNode = null;
        }

        private (NodeViewModel node, int port) FindNodeAndPortAtPoint(Point point)
        {
            foreach (var node in ViewModel.Nodes)
            {
                // Check if point is near any port
                for (int port = 0; port < 4; port++)
                {
                    var portPos = node.GetPortPosition(port);
                    double distance = Math.Sqrt(
                        Math.Pow(point.X - portPos.X, 2) +
                        Math.Pow(point.Y - portPos.Y, 2));

                    if (distance < 15) // Tolerance of 15 pixels
                    {
                        return (node, port);
                    }
                }

                // If not near a port, check if inside the node and pick nearest port
                Rect nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
                if (nodeRect.Contains(point))
                {
                    // Find nearest port
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

        #endregion

        #region Keyboard Operations

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null) return;

            if (e.Key == Key.Delete)
            {
                ViewModel.DeleteSelectedCommand.Execute(null);
                UpdateStatus("Deleted selected items");
                e.Handled = true;
            }
            else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // TODO: Undo
                UpdateStatus("Undo");
                e.Handled = true;
            }
            else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // TODO: Redo
                UpdateStatus("Redo");
                e.Handled = true;
            }
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // TODO: Copy
                UpdateStatus("Copy");
                e.Handled = true;
            }
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // TODO: Paste
                UpdateStatus("Paste");
                e.Handled = true;
            }
        }

        #endregion

        #region File Operations

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                var dto = ConvertToDto();
                string json = SerializeToJson(dto);
                File.WriteAllText(dialog.FileName, json);
                UpdateStatus($"Saved to {dialog.FileName}");
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                string json = File.ReadAllText(dialog.FileName);
                var dto = DeserializeFromJson(json);
                LoadFromDto(dto);
                UpdateStatus($"Loaded from {dialog.FileName}");
            }
        }

        private Models.DiagramDto ConvertToDto()
        {
            var dto = new Models.DiagramDto
            {
                ZoomLevel = ViewModel.ZoomScale
            };

            foreach (var node in ViewModel.Nodes)
            {
                dto.Nodes.Add(new Models.NodeDto
                {
                    Id = node.Id,
                    X = node.X,
                    Y = node.Y,
                    Width = node.Width,
                    Height = node.Height,
                    Text = node.Text,
                    Color = (node.Background as SolidColorBrush)?.Color.ToString() ?? "LightBlue"
                });
            }

            foreach (var conn in ViewModel.Connectors)
            {
                dto.Connections.Add(new Models.ConnectionDto
                {
                    Id = conn.Id,
                    SourceNodeId = conn.SourceNode.Id,
                    SourcePort = conn.SourcePort,
                    TargetNodeId = conn.TargetNode.Id,
                    TargetPort = conn.TargetPort
                });
            }

            return dto;
        }

        private void LoadFromDto(Models.DiagramDto dto)
        {
            ViewModel.Nodes.Clear();
            ViewModel.Connectors.Clear();
            ViewModel.SelectedNodes.Clear();

            var nodeMap = new Dictionary<Guid, NodeViewModel>();

            foreach (var nodeDto in dto.Nodes)
            {
                var node = new NodeViewModel
                {
                    Id = nodeDto.Id,
                    X = nodeDto.X,
                    Y = nodeDto.Y,
                    Width = nodeDto.Width,
                    Height = nodeDto.Height,
                    Text = nodeDto.Text
                };

                if (!string.IsNullOrEmpty(nodeDto.Color))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(nodeDto.Color);
                        node.Background = new SolidColorBrush(color);
                    }
                    catch { }
                }

                ViewModel.Nodes.Add(node);
                nodeMap[node.Id] = node;
            }

            foreach (var connDto in dto.Connections)
            {
                if (nodeMap.TryGetValue(connDto.SourceNodeId, out var sourceNode) &&
                    nodeMap.TryGetValue(connDto.TargetNodeId, out var targetNode))
                {
                    var connector = new ConnectorViewModel(
                        sourceNode, connDto.SourcePort,
                        targetNode, connDto.TargetPort)
                    {
                        Id = connDto.Id
                    };
                    ViewModel.Connectors.Add(connector);
                }
            }

            ViewModel.ZoomScale = dto.ZoomLevel > 0 ? dto.ZoomLevel : 1.0;
        }

        // Simple JSON serialization without external dependencies
        private string SerializeToJson(Models.DiagramDto dto)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"ZoomLevel\": {dto.ZoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            
            // Nodes
            sb.AppendLine("  \"Nodes\": [");
            for (int i = 0; i < dto.Nodes.Count; i++)
            {
                var node = dto.Nodes[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Id\": \"{node.Id}\",");
                sb.AppendLine($"      \"X\": {node.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"      \"Y\": {node.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"      \"Width\": {node.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"      \"Height\": {node.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"      \"Text\": \"{EscapeJsonString(node.Text)}\",");
                sb.AppendLine($"      \"Color\": \"{EscapeJsonString(node.Color)}\"");
                sb.Append("    }");
                if (i < dto.Nodes.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ],");

            // Connections
            sb.AppendLine("  \"Connections\": [");
            for (int i = 0; i < dto.Connections.Count; i++)
            {
                var conn = dto.Connections[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Id\": \"{conn.Id}\",");
                sb.AppendLine($"      \"SourceNodeId\": \"{conn.SourceNodeId}\",");
                sb.AppendLine($"      \"SourcePort\": {conn.SourcePort},");
                sb.AppendLine($"      \"TargetNodeId\": \"{conn.TargetNodeId}\",");
                sb.AppendLine($"      \"TargetPort\": {conn.TargetPort}");
                sb.Append("    }");
                if (i < dto.Connections.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");

            sb.AppendLine("}");
            return sb.ToString();
        }

        private Models.DiagramDto DeserializeFromJson(string json)
        {
            // Simple manual JSON parsing for .NET Framework 4.7.2 compatibility
            var dto = new Models.DiagramDto();

            try
            {
                // Parse ZoomLevel
                var zoomMatch = System.Text.RegularExpressions.Regex.Match(json, @"""ZoomLevel""\s*:\s*([\d.]+)");
                if (zoomMatch.Success)
                {
                    dto.ZoomLevel = double.Parse(zoomMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                }

                // Parse Nodes array
                var nodesMatch = System.Text.RegularExpressions.Regex.Match(json, @"""Nodes""\s*:\s*\[([\s\S]*?)\](?=\s*,\s*""Connections""|$)");
                if (nodesMatch.Success)
                {
                    var nodesContent = nodesMatch.Groups[1].Value;
                    var nodeMatches = System.Text.RegularExpressions.Regex.Matches(nodesContent, @"\{[^{}]*\}");
                    
                    foreach (System.Text.RegularExpressions.Match nodeMatch in nodeMatches)
                    {
                        var nodeJson = nodeMatch.Value;
                        var nodeDto = new Models.NodeDto
                        {
                            Id = ParseGuid(nodeJson, "Id"),
                            X = ParseDouble(nodeJson, "X"),
                            Y = ParseDouble(nodeJson, "Y"),
                            Width = ParseDouble(nodeJson, "Width"),
                            Height = ParseDouble(nodeJson, "Height"),
                            Text = ParseString(nodeJson, "Text"),
                            Color = ParseString(nodeJson, "Color")
                        };
                        dto.Nodes.Add(nodeDto);
                    }
                }

                // Parse Connections array
                var connsMatch = System.Text.RegularExpressions.Regex.Match(json, @"""Connections""\s*:\s*\[([\s\S]*?)\]\s*\}");
                if (connsMatch.Success)
                {
                    var connsContent = connsMatch.Groups[1].Value;
                    var connMatches = System.Text.RegularExpressions.Regex.Matches(connsContent, @"\{[^{}]*\}");
                    
                    foreach (System.Text.RegularExpressions.Match connMatch in connMatches)
                    {
                        var connJson = connMatch.Value;
                        var connDto = new Models.ConnectionDto
                        {
                            Id = ParseGuid(connJson, "Id"),
                            SourceNodeId = ParseGuid(connJson, "SourceNodeId"),
                            SourcePort = ParseInt(connJson, "SourcePort"),
                            TargetNodeId = ParseGuid(connJson, "TargetNodeId"),
                            TargetPort = ParseInt(connJson, "TargetPort")
                        };
                        dto.Connections.Add(connDto);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parse error: {ex.Message}");
            }

            return dto;
        }

        private Guid ParseGuid(string json, string propertyName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, $@"""{propertyName}""\s*:\s*""([^""]+)""");
            return match.Success ? Guid.Parse(match.Groups[1].Value) : Guid.Empty;
        }

        private double ParseDouble(string json, string propertyName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, $@"""{propertyName}""\s*:\s*([\d.-]+)");
            return match.Success ? double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        }

        private int ParseInt(string json, string propertyName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, $@"""{propertyName}""\s*:\s*(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private string ParseString(string json, string propertyName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, $@"""{propertyName}""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
            if (!match.Success) return "";
            return match.Groups[1].Value
                .Replace("\\\\", "\\")
                .Replace("\\\"", "\"")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        private T FindParentDataContext<T>(DependencyObject child) where T : class
        {
            var parent = child;
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is T dataContext)
                    return dataContext;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        #endregion
    }
}
