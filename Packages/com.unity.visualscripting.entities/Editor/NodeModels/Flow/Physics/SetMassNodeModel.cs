using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Physics/" + k_Title)]
    class SetMassNodeModel : DotsNodeModel<SetMass>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort, IHasMainInputPort
    {
        const string k_Title = "Set Mass";
        public override string Title => k_Title;
        public IPortModel ExecutionInputPort { get; set; }

        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
#endif
}
