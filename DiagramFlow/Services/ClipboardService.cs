using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DiagramFlow.Models;
using DiagramFlow.ViewModels;
using DiagramFlow.Helpers;
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

            // 選択されたノードを追加
            foreach (var node in nodesList)
            {
                dto.Nodes.Add(node.ToDto());
            }

            // 両端が選択範囲内にある場合、接続を追加
            if (allConnectors != null)
            {
                foreach (var conn in allConnectors)
                {
                    if (nodesList.Contains(conn.SourceNode) &&
                        nodesList.Contains(conn.TargetNode))
                    {
                        dto.Connections.Add(conn.ToDto());
                    }
                }
            }

            var json = JsonConvert.SerializeObject(dto);
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

                // 貼り付けるアイテムをbasePositionに配置するためのオフセットを決定 (左上揃え)
                double minX = dto.Nodes.Min(n => n.X);
                double minY = dto.Nodes.Min(n => n.Y);

                double offsetX = basePosition.X - minX;
                double offsetY = basePosition.Y - minY;

                // 古いIDから新しいViewModelへのマッピング
                var idMap = new Dictionary<Guid, NodeViewModel>();

                foreach (var nodeDto in dto.Nodes)
                {
                    var newNode = new NodeViewModel
                    {
                        X = nodeDto.X + offsetX,
                        Y = nodeDto.Y + offsetY,
                        Width = nodeDto.Width,
                        Height = nodeDto.Height,
                        Text = nodeDto.Text,
                        ShapeType = nodeDto.ShapeType
                    };

                    // 色を保持
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
                // 無効なクリップボードの内容を無視
                // 実際のアプリではログに記録することもできます
            }

            return result;
        }
    }
}
