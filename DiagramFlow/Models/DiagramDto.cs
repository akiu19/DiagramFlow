using System.Collections.Generic;

namespace DiagramFlow.Models
{
    public class DiagramDto
    {
        public List<NodeDto> Nodes { get; set; } = new List<NodeDto>();
        public List<ConnectionDto> Connections { get; set; } = new List<ConnectionDto>();
        public double ZoomLevel { get; set; } = 1.0;
    }
}
