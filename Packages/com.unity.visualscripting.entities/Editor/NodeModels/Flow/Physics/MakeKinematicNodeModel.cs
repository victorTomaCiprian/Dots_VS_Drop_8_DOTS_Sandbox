using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Physics/" + k_Title)]
    class MakeKinematicNodeModel : DotsNodeModel<MakeKinematic>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort
    {
        const string k_Title = "Make Kinematic";
        public override string Title => k_Title;
        public IPortModel ExecutionInputPort { get; set; }

        public IPortModel ExecutionOutputPort { get; set; }
    }
#endif
}
