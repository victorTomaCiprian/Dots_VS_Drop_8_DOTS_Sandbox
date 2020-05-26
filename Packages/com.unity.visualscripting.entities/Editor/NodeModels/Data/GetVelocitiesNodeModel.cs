using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Physics/" + k_Title)]
    class GetVelocitiesNodeModel : DotsNodeModel<GetVelocities>, IHasMainInputPort
    {
        const string k_Title = "Get Velocities";

        public override string Title => k_Title;

        public IPortModel InputPort { get; set; }
    }
#endif
}
