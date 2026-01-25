using System.Collections.Generic;
using DiagramFlow.ViewModels;
using System.Windows;

namespace DiagramFlow.Services
{
    public class PasteResult
    {
        public List<NodeViewModel> Nodes { get; set; } = new List<NodeViewModel>();
        public List<ConnectorViewModel> Connectors { get; set; } = new List<ConnectorViewModel>();
    }

    public interface IClipboardService
    {
        void Copy(IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectorViewModel> allConnectors);
        PasteResult Paste(Point centerPosition);
    }
}
