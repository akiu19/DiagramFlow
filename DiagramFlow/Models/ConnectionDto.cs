using System;

namespace DiagramFlow.Models
{
    public class ConnectionDto
    {
        public Guid Id { get; set; }
        public Guid SourceNodeId { get; set; }
        public int SourcePort { get; set; } // 0: 上, 1: 下, 2: 左, 3: 右（例）
        public Guid TargetNodeId { get; set; }
        public int TargetPort { get; set; }
    }
}
