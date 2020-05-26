using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Clamp")]
    class ClampNodeModel : DotsNodeModel<Clamp>, IHasMainInputPort, IHasMainOutputPort
    {
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
