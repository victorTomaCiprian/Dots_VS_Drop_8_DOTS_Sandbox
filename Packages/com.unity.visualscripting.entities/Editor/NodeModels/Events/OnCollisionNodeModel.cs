using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Events/" + k_Title)]
    class OnCollisionNodeModel : DotsNodeModel<OnCollision>, IHasMainExecutionOutputPort
    {
        const string k_Title = "On Collision";

        public override string Title => k_Title;

        public IPortModel ExecutionOutputPort { get; set; }
    }
#endif
}
