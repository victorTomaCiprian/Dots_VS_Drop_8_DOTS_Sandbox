using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Disable")]
    class DisableNodeModel : DotsNodeModel<Disable>, IHasMainExecutionInputPort, IHasMainInputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
