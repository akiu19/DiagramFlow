using System.Windows.Media;
using DiagramFlow.Models;
using DiagramFlow.ViewModels;

namespace DiagramFlow.Helpers
{
    public static class DtoHelper
    {
        public static NodeDto ToDto(this NodeViewModel node)
        {
            return new NodeDto
            {
                Id = node.Id,
                X = node.X,
                Y = node.Y,
                Width = node.Width,
                Height = node.Height,
                Text = node.Text,
                Color = (node.Background as SolidColorBrush)?.Color.ToString() ?? "LightBlue",
                ShapeType = node.ShapeType
            };
        }

        public static ConnectionDto ToDto(this ConnectorViewModel conn)
        {
            return new ConnectionDto
            {
                Id = conn.Id,
                SourceNodeId = conn.SourceNode.Id,
                SourcePort = conn.SourcePort,
                TargetNodeId = conn.TargetNode.Id,
                TargetPort = conn.TargetPort
            };
        }
    }
}
