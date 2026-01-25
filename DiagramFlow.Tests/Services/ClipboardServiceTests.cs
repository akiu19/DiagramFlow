using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DiagramFlow.Services;
using DiagramFlow.ViewModels;
using Newtonsoft.Json;
using Xunit;

namespace DiagramFlow.Tests.Services
{
    public class ClipboardServiceTests
    {
        private class TestSystemClipboard : ISystemClipboard
        {
            private string _text;

            public void SetText(string text) => _text = text;
            public string GetText() => _text;
            public bool ContainsText() => !string.IsNullOrEmpty(_text);
        }

        [Fact]
        public void Copy_ShouldSerializeNodesToClipboard()
        {
            var clipboard = new TestSystemClipboard();
            var service = new ClipboardService(clipboard);

            var node1 = new NodeViewModel { Text = "Node1", X = 10, Y = 10 };
            var nodes = new List<NodeViewModel> { node1 };
            
            service.Copy(nodes, new List<ConnectorViewModel>());

            string json = clipboard.GetText();
            Assert.Contains("Node1", json);
            
            // Validate content by deserializing back
            var dto = JsonConvert.DeserializeObject<DiagramFlow.Models.DiagramDto>(json);
            Assert.Single(dto.Nodes);
            Assert.Equal(10.0, dto.Nodes[0].X);
        }

        [Fact]
        public void Copy_ShouldIncludeConnectors_WhenBothEndsSelected()
        {
            var clipboard = new TestSystemClipboard();
            var service = new ClipboardService(clipboard);

            var node1 = new NodeViewModel { Text = "Node1" };
            var node2 = new NodeViewModel { Text = "Node2" };
            var node3 = new NodeViewModel { Text = "Node3" };

            var conn12 = new ConnectorViewModel(node1, 0, node2, 0); // Both selected
            var conn23 = new ConnectorViewModel(node2, 0, node3, 0); // Only source selected

            var selectedNodes = new List<NodeViewModel> { node1, node2 };
            var allConnectors = new List<ConnectorViewModel> { conn12, conn23 };

            service.Copy(selectedNodes, allConnectors);

            string json = clipboard.GetText();
            Assert.Contains(conn12.Id.ToString(), json);
            Assert.DoesNotContain(conn23.Id.ToString(), json);
        }

        [Fact]
        public void Paste_ShouldDeserializeAndRemapIds()
        {
            var clipboard = new TestSystemClipboard();
            var service = new ClipboardService(clipboard);

            // Setup clipboard with valid JSON
            var oldId1 = Guid.NewGuid();
            var oldId2 = Guid.NewGuid();
            var json = $@"{{
                ""Nodes"": [
                    {{ ""Id"": ""{oldId1}"", ""X"": 0, ""Y"": 0, ""Text"": ""N1"" }},
                    {{ ""Id"": ""{oldId2}"", ""X"": 100, ""Y"": 0, ""Text"": ""N2"" }}
                ],
                ""Connections"": [
                    {{ ""SourceNodeId"": ""{oldId1}"", ""TargetNodeId"": ""{oldId2}"", ""SourcePort"": 0, ""TargetPort"": 1 }}
                ]
            }}";
            clipboard.SetText(json);

            // Execute Paste
            var result = service.Paste(new Point(50, 50));

            // Verify
            Assert.Equal(2, result.Nodes.Count);
            Assert.Single(result.Connectors);

            var n1 = result.Nodes.FirstOrDefault(n => n.Text == "N1");
            var n2 = result.Nodes.FirstOrDefault(n => n.Text == "N2");

            Assert.NotNull(n1);
            Assert.NotNull(n2);
            Assert.NotEqual(oldId1, n1.Id); // Should have new ID
            Assert.NotEqual(oldId2, n2.Id);

            // Check offset: Original local (0,0) -> Base(50,50) since minX=0, minY=0
            Assert.Equal(50, n1.X);
            Assert.Equal(50, n1.Y);
            
            // Original local(100,0) -> Base(150, 50)
            Assert.Equal(150, n2.X);
            Assert.Equal(50, n2.Y);

            // Verify connection linking
            var conn = result.Connectors[0];
            Assert.Same(n1, conn.SourceNode);
            Assert.Same(n2, conn.TargetNode);
        }

        [Fact]
        public void Paste_ShouldReturnEmpty_WhenClipboardEmpty()
        {
            var clipboard = new TestSystemClipboard();
            var service = new ClipboardService(clipboard);

            var result = service.Paste(new Point(0, 0));

            Assert.Empty(result.Nodes);
        }
    }
}
