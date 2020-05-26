using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class GetScaleNodeModel : DotsNodeModel<GetScale>, IHasMainInputPort
    {
        const string k_Title = "Get Scale";

        public override string Title => k_Title;

        public IPortModel InputPort { get; set; }
    }
}
