using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Events/" + k_Title)]
    class OnTriggerNodeModel : DotsNodeModel<OnTrigger>, IHasMainExecutionOutputPort
    {
        const string k_Title = "On Trigger";

        public override string Title => k_Title;

        public IPortModel ExecutionOutputPort { get; set; }
    }
#endif
}
