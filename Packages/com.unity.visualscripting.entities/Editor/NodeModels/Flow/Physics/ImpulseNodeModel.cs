using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Physics/" + k_Title)]
    class ImpulseNodeModel : DotsNodeModel<Impulse>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort
    {
        const string k_Title = "Impulse";
        public override string Title => k_Title;
        public IPortModel ExecutionInputPort { get; set; }

        public IPortModel ExecutionOutputPort { get; set; }
    }
#endif
}
