using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Destroy")]
    class DestroyNodeModel : DotsNodeModel<Destroy>, IHasMainExecutionInputPort, IHasMainInputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
