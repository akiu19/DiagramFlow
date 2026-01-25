using System;

namespace DiagramFlow.Models
{
    public class ConnectionDto
    {
        public Guid Id { get; set; }
        public Guid SourceNodeId { get; set; }
        public int SourcePort { get; set; } // 0: Top, 1: Bottom, 2: Left, 3: Right (Example)
        public Guid TargetNodeId { get; set; }
        public int TargetPort { get; set; }
    }
}
