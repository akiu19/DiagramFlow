using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DiagramFlow.Services;

namespace DiagramFlow.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Node and Connector collections
        public ObservableCollection<NodeViewModel> Nodes { get; } = new ObservableCollection<NodeViewModel>();
        public ObservableCollection<ConnectorViewModel> Connectors { get; } = new ObservableCollection<ConnectorViewModel>();

        // Zoom control (range: 0.1 to 4.0)
        private double _zoomScale = 1.0;
        public double ZoomScale
        {
            get => _zoomScale;
            set
            {
                if (Math.Abs(_zoomScale - value) > 0.0001)
                {
                    _zoomScale = Math.Max(0.1, Math.Min(4.0, value));
                    OnPropertyChanged();
                }
            }
        }

        // Selected nodes
        public ObservableCollection<NodeViewModel> SelectedNodes { get; } = new ObservableCollection<NodeViewModel>();
        // Selected connectors
        public ObservableCollection<ConnectorViewModel> SelectedConnectors { get; } = new ObservableCollection<ConnectorViewModel>();

        // Commands
        public ICommand AddNodeCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }

        public UndoService UndoService { get; } = new UndoService();
        public IClipboardService ClipboardService { get; set; } = new ClipboardService(new SystemClipboardWrapper());

        public MainViewModel()
        {
            AddNodeCommand = new RelayCommand(_ => AddNode());
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected());
            UndoCommand = new RelayCommand(_ => UndoService.Undo(), _ => UndoService.CanUndo);
            RedoCommand = new RelayCommand(_ => UndoService.Redo(), _ => UndoService.CanRedo);
            CopyCommand = new RelayCommand(_ => CopySelection());
            PasteCommand = new RelayCommand(p => PasteFromClipboard(p));

            UndoService.StateChanged += (s, e) =>
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            };

            // Add sample nodes for testing
            Nodes.Add(new NodeViewModel
            {
                X = 200,
                Y = 150,
                Width = 120,
                Height = 80,
                Text = "Node 1"
            });

            Nodes.Add(new NodeViewModel
            {
                X = 400,
                Y = 300,
                Width = 120,
                Height = 80,
                Text = "Node 2"
            });
        }

        private void AddNode()
        {
            var node = new NodeViewModel
            {
                X = 100 + (Nodes.Count * 20),
                Y = 100 + (Nodes.Count * 20),
                Text = $"Node {Nodes.Count + 1}"
            };
            // Use UndoService
            UndoService.Execute(new AddNodeCommand(this, node));
        }

        private void DeleteSelected()
        {
            var nodesToDelete = SelectedNodes.ToList();
            var connectorsToDelete = new HashSet<ConnectorViewModel>(SelectedConnectors);

            foreach (var node in nodesToDelete)
            {
                var relatedConnectors = Connectors
                    .Where(c => c.SourceNode == node || c.TargetNode == node);
                foreach (var conn in relatedConnectors)
                {
                    connectorsToDelete.Add(conn);
                }
            }

            if (nodesToDelete.Any() || connectorsToDelete.Any())
            {
                UndoService.Execute(new DeleteItemsCommand(this, nodesToDelete, connectorsToDelete));
            }
        }

        public void ClearSelection()
        {
            foreach (var node in SelectedNodes.ToList())
            {
                node.IsSelected = false;
            }
            SelectedNodes.Clear();

            foreach (var conn in SelectedConnectors.ToList())
            {
                conn.IsSelected = false;
            }
            SelectedConnectors.Clear();
        }

        public void SelectNode(NodeViewModel node, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                ClearSelection();
            }

            if (!SelectedNodes.Contains(node))
            {
                node.IsSelected = true;
                SelectedNodes.Add(node);
            }
        }

        public void ToggleNodeSelection(NodeViewModel node)
        {
            if (SelectedNodes.Contains(node))
            {
                node.IsSelected = false;
                SelectedNodes.Remove(node);
            }
            else
            {
                node.IsSelected = true;
                SelectedNodes.Add(node);
            }
        }

        public void SelectConnector(ConnectorViewModel connector, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                ClearSelection();
            }

            if (!SelectedConnectors.Contains(connector))
            {
                connector.IsSelected = true;
                SelectedConnectors.Add(connector);
            }
        }

        public void MoveSelectedNodes(double deltaX, double deltaY)
        {
            foreach (var node in SelectedNodes)
            {
                node.X += deltaX;
                node.Y += deltaY;
            }
        }

        private void CopySelection()
        {
            ClipboardService.Copy(SelectedNodes, Connectors);
        }

        private void PasteFromClipboard(object parameter)
        {
            Point position = new Point(100, 100);
            if (parameter is Point p)
            {
                position = p;
            }

            var result = ClipboardService.Paste(position);
            if (result.Nodes.Any())
            {
                UndoService.Execute(new AddItemsCommand(this, result.Nodes, result.Connectors));
            }
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
    }
}

