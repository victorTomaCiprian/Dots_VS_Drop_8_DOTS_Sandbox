using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
#if VS_DOTS_PHYSICS_EXISTS
    [Serializable, DotsSearcherItem("Physics/" + k_Title)]
    class GetGravityFactorNodeModel : DotsNodeModel<GetGravityFactor>, IHasMainInputPort
    {
        const string k_Title = "Get Gravity Factor";

        public override string Title => k_Title;

        public IPortModel InputPort { get; set; }
    }
#endif
}
