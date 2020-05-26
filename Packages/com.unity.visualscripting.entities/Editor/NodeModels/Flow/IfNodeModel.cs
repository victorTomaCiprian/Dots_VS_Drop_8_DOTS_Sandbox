using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Flow/If")]
    class IfNodeModel : DotsNodeModel<If>, IHasMainInputPort, IHasMainExecutionInputPort, IHasMainExecutionOutputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
