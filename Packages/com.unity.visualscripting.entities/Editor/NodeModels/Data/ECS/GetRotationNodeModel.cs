using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class GetRotationNodeModel : DotsNodeModel<GetRotation>, IHasMainInputPort
    {
        const string k_Title = "Get Rotation";

        public override string Title => k_Title;

        public IPortModel InputPort { get; set; }
    }
}
