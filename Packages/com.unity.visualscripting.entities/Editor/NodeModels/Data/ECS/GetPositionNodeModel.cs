using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class GetPositionNodeModel : DotsNodeModel<GetPosition>, IHasMainInputPort
    {
        const string k_Title = "Get Position";

        public override string Title => k_Title;

        public IPortModel InputPort { get; set; }
    }
}
