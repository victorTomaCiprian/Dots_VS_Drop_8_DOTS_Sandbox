using System;
using System.Collections.Generic;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    public interface IDotsNodeModel : INodeModel
    {
        Type NodeType { get; }
        INode Node { get; }
        Dictionary<string, uint> PortToOffsetMapping { get; }
    }
}
