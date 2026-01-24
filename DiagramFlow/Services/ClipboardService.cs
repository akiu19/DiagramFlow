using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DiagramFlow.Models;
using DiagramFlow.ViewModels;
using Newtonsoft.Json;

namespace DiagramFlow.Services
{
    public class ClipboardService : IClipboardService
    {
        private readonly ISystemClipboard _clipboard;

        public ClipboardService(ISystemClipboard clipboard)
        {
            _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        }

        public void Copy(IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectorViewModel> allConnectors)
        {
            var nodesList = nodes.ToList();
            if (!nodesList.Any()) return;

            var dto = new DiagramDto();

            // Add selected nodes
            foreach (var node in nodesList)
            {
                dto.Nodes.Add(new NodeDto
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

            // Add connections if both ends are in the selection
            if (allConnectors != null)
            {
                foreach (var conn in allConnectors)
                {
                    if (nodesList.Contains(conn.SourceNode) &&
                        nodesList.Contains(conn.TargetNode))
                    {
                        dto.Connections.Add(new ConnectionDto
                        {
                            Id = conn.Id,
                            SourceNodeId = conn.SourceNode.Id,
                            SourcePort = conn.SourcePort,
                            TargetNodeId = conn.TargetNode.Id,
                            TargetPort = conn.TargetPort
                        });
                    }
                }
            }

            string json = JsonConvert.SerializeObject(dto);
            _clipboard.SetText(json);
        }

        public PasteResult Paste(Point basePosition)
        {
            var result = new PasteResult();

            if (!_clipboard.ContainsText()) return result;

            string json = _clipboard.GetText();
            if (string.IsNullOrWhiteSpace(json)) return result;

            try
            {
                var dto = JsonConvert.DeserializeObject<DiagramDto>(json);
                if (dto == null || !dto.Nodes.Any()) return result;

                // Determine offset to place pasted items at basePosition (top-left aligned)
                double minX = dto.Nodes.Min(n => n.X);
                double minY = dto.Nodes.Min(n => n.Y);

                double offsetX = basePosition.X - minX;
                double offsetY = basePosition.Y - minY;

                // Mapping from Old IDs to New ViewModels
                var idMap = new Dictionary<Guid, NodeViewModel>();

                foreach (var nodeDto in dto.Nodes)
                {
                    var newNode = new NodeViewModel
                    {
                        X = nodeDto.X + offsetX,
                        Y = nodeDto.Y + offsetY,
                        Width = nodeDto.Width,
                        Height = nodeDto.Height,
                        Text = nodeDto.Text
                    };

                    // Preserve color
                    if (!string.IsNullOrEmpty(nodeDto.Color))
                    {
                        try
                        {
                            var color = (Color)ColorConverter.ConvertFromString(nodeDto.Color);
                            newNode.Background = new SolidColorBrush(color);
                        }
                        catch { }
                    }

                    result.Nodes.Add(newNode);
                    idMap[nodeDto.Id] = newNode;
                }

                foreach (var connDto in dto.Connections)
                {
                    if (idMap.TryGetValue(connDto.SourceNodeId, out var sourceNode) &&
                        idMap.TryGetValue(connDto.TargetNodeId, out var targetNode))
                    {
                        var newConn = new ConnectorViewModel(
                            sourceNode, connDto.SourcePort,
                            targetNode, connDto.TargetPort);
                        result.Connectors.Add(newConn);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore invalid clipboard content
                // In a real app we might want to log this
            }

            return result;
        }
    }
}
