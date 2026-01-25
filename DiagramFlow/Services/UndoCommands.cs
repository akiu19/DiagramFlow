using System.Collections.Generic;
using System.Linq;
using DiagramFlow.ViewModels;

namespace DiagramFlow.Services
{
    public class MoveNodesCommand : IUndoableCommand
    {
        private readonly List<NodeViewModel> _nodes;
        private readonly double _dx;
        private readonly double _dy;

        public string Name => "Move Nodes";

        public MoveNodesCommand(IEnumerable<NodeViewModel> nodes, double dx, double dy)
        {
            _nodes = nodes.ToList();
            _dx = dx;
            _dy = dy;
        }

        public void Execute()
        {
            foreach (var node in _nodes)
            {
                node.X += _dx;
                node.Y += _dy;
            }
        }

        public void Undo()
        {
            foreach (var node in _nodes)
            {
                node.X -= _dx;
                node.Y -= _dy;
            }
        }
    }

    public class AddNodeCommand : IUndoableCommand
    {
        private readonly MainViewModel _vm;
        private readonly NodeViewModel _node;

        public string Name => "Add Node";

        public AddNodeCommand(MainViewModel vm, NodeViewModel node)
        {
            _vm = vm;
            _node = node;
        }

        public void Execute()
        {
            if (!_vm.Nodes.Contains(_node))
            {
                _vm.Nodes.Add(_node);
            }
        }

        public void Undo()
        {
            _vm.Nodes.Remove(_node);
            _vm.SelectedNodes.Remove(_node);
        }
    }

    public class AddConnectionCommand : IUndoableCommand
    {
        private readonly MainViewModel _vm;
        private readonly ConnectorViewModel _connector;

        public string Name => "Add Connection";

        public AddConnectionCommand(MainViewModel vm, ConnectorViewModel connector)
        {
            _vm = vm;
            _connector = connector;
        }

        public void Execute()
        {
            if (!_vm.Connectors.Contains(_connector))
            {
                _vm.Connectors.Add(_connector);
            }
        }

        public void Undo()
        {
            _vm.Connectors.Remove(_connector);
            _vm.SelectedConnectors.Remove(_connector);
        }
    }

    public class DeleteItemsCommand : IUndoableCommand
    {
        private readonly MainViewModel _vm;
        private readonly List<NodeViewModel> _nodesToDelete;
        private readonly List<ConnectorViewModel> _connectorsToDelete;

        public string Name => "Delete Items";

        public DeleteItemsCommand(MainViewModel vm, IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectorViewModel> connectors)
        {
            _vm = vm;
            _nodesToDelete = nodes.ToList();
            _connectorsToDelete = connectors.ToList();
        }

        public void Execute()
        {
            foreach (var conn in _connectorsToDelete)
            {
                _vm.Connectors.Remove(conn);
                _vm.SelectedConnectors.Remove(conn);
            }
            foreach (var node in _nodesToDelete)
            {
                _vm.Nodes.Remove(node);
                _vm.SelectedNodes.Remove(node);
            }
        }

        public void Undo()
        {
            foreach (var node in _nodesToDelete)
            {
                _vm.Nodes.Add(node);
            }
            foreach (var conn in _connectorsToDelete)
            {
                _vm.Connectors.Add(conn);
            }
        }
    }

    public class ResizeNodeCommand : IUndoableCommand
    {
        private readonly NodeViewModel _node;
        private readonly double _oldWidth;
        private readonly double _oldHeight;
        private readonly double _newWidth;
        private readonly double _newHeight;

        public string Name => "Resize Node";

        public ResizeNodeCommand(NodeViewModel node, double oldWidth, double oldHeight, double newWidth, double newHeight)
        {
            _node = node;
            _oldWidth = oldWidth;
            _oldHeight = oldHeight;
            _newWidth = newWidth;
            _newHeight = newHeight;
        }

        public void Execute()
        {
            _node.Width = _newWidth;
            _node.Height = _newHeight;
        }

        public void Undo()
        {
            _node.Width = _oldWidth;
            _node.Height = _oldHeight;
        }
    }

    public class EditTextCommand : IUndoableCommand
    {
        private readonly NodeViewModel _node;
        private readonly string _oldText;
        private readonly string _newText;

        public string Name => "Edit Text";

        public EditTextCommand(NodeViewModel node, string oldText, string newText)
        {
            _node = node;
            _oldText = oldText;
            _newText = newText;
        }

        public void Execute()
        {
            _node.Text = _newText;
        }

        public void Undo()
        {
            _node.Text = _oldText;
        }
    }

    public class AddItemsCommand : IUndoableCommand
    {
        private readonly MainViewModel _vm;
        private readonly List<NodeViewModel> _nodes;
        private readonly List<ConnectorViewModel> _connectors;

        public string Name => "Add Items";

        public AddItemsCommand(MainViewModel vm, IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectorViewModel> connectors = null)
        {
            _vm = vm;
            _nodes = nodes.ToList();
            _connectors = connectors?.ToList() ?? new List<ConnectorViewModel>();
        }

        public void Execute()
        {
            foreach (var node in _nodes)
            {
                if (!_vm.Nodes.Contains(node)) _vm.Nodes.Add(node);
            }
            foreach (var conn in _connectors)
            {
                if (!_vm.Connectors.Contains(conn)) _vm.Connectors.Add(conn);
            }
            
            _vm.ClearSelection();
            foreach (var node in _nodes) _vm.SelectNode(node, true);
            foreach (var conn in _connectors) _vm.SelectConnector(conn, true);
        }

        public void Undo()
        {
            foreach (var conn in _connectors) _vm.Connectors.Remove(conn);
            foreach (var node in _nodes) _vm.Nodes.Remove(node);
            
            _vm.ClearSelection();
        }
    }
}