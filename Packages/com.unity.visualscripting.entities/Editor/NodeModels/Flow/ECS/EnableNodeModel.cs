using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Enable")]
    class EnableNodeModel : DotsNodeModel<Enable>, IHasMainExecutionInputPort, IHasMainInputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
