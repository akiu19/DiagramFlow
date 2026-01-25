using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using DiagramFlow.ViewModels;
using DiagramFlow.Services;
using DiagramFlow.Helpers;
using System.Linq;
using Newtonsoft.Json;

namespace DiagramFlow
{
    public partial class MainWindow : Window
    {
        private enum OperationState
        {
            Idle,
            ResizingNode
        }

        private OperationState _currentState = OperationState.Idle;

        // Resize operation
        private Point _resizeStartSize;
        private NodeViewModel _resizingNode;
        private string _originalNodeText;

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

        #region Node Text Editing

        private void NodeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var node = textBox?.DataContext as NodeViewModel;
            if (node != null)
            {
                _originalNodeText = node.Text;
            }
        }

        private void NodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            var node = textBox?.DataContext as NodeViewModel;
            if (node == null) return;

            if (e.Key == Key.Enter)
            {
                // Commit
                if (_originalNodeText != node.Text)
                {
                    ViewModel.UndoService.AddToHistory(
                        new EditTextCommand(node, _originalNodeText, node.Text));
                }
                node.IsEditing = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Revert
                node.Text = _originalNodeText;
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
                if (node.IsEditing) // If still editing (not cancelled by Esc)
                {
                    if (_originalNodeText != node.Text)
                    {
                         ViewModel.UndoService.AddToHistory(
                            new EditTextCommand(node, _originalNodeText, node.Text));
                    }
                    node.IsEditing = false;
                }
            }
        }

        #endregion

        #region Resize Operations

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            var thumb = sender as Thumb;
            var node = VisualHelper.FindParentDataContext<NodeViewModel>(thumb);
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
            if (_resizingNode != null && ViewModel != null)
            {
                if (Math.Abs(_resizingNode.Width - _resizeStartSize.X) > 0.1 ||
                    Math.Abs(_resizingNode.Height - _resizeStartSize.Y) > 0.1)
                {
                    ViewModel.UndoService.AddToHistory(
                        new ResizeNodeCommand(_resizingNode, 
                            _resizeStartSize.X, _resizeStartSize.Y, 
                            _resizingNode.Width, _resizingNode.Height));
                }
            }

            _currentState = OperationState.Idle;
            _resizingNode = null;
            e.Handled = true;
        }

        #endregion

        #region Connector Operations
        
        private void Connector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentState != OperationState.Idle) return;

            var line = sender as Line;
            var connector = line?.DataContext as ConnectorViewModel;
            if (connector == null || ViewModel == null) return;

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
                ViewModel.UndoCommand.Execute(null);
                UpdateStatus("Undo");
                e.Handled = true;
            }
            else if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ViewModel.RedoCommand.Execute(null);
                UpdateStatus("Redo");
                e.Handled = true;
            }
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ViewModel?.CopyCommand.CanExecute(null) == true)
                {
                    ViewModel.CopyCommand.Execute(null);
                    UpdateStatus("Copy");
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var mousePos = Mouse.GetPosition(DiagramCanvas);
                if (ViewModel?.PasteCommand.CanExecute(mousePos) == true)
                {
                    ViewModel.PasteCommand.Execute(mousePos);
                    UpdateStatus("Paste");
                    e.Handled = true;
                }
            }
        }
        
        #endregion

        #region Clipboard Operations

        // Logic moved to ClipboardService and MainViewModel

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
                try {
                    ViewModel.Save(dialog.FileName);
                    UpdateStatus($"Saved to {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
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
                try
                {
                    ViewModel.Load(dialog.FileName);
                    UpdateStatus($"Loaded from {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}");
                }
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        #endregion
    }
}