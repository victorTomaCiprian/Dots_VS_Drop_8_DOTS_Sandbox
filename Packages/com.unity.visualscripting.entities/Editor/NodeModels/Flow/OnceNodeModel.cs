using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Flow/Once")]
    class OnceNodeModel : DotsNodeModel<Once>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
    }
}
