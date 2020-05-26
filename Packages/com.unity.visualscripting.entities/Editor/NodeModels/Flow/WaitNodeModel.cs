using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Flow/Wait")]
    class WaitNodeModel : DotsNodeModel<Wait>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort,
        IHasMainInputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
