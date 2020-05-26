using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace NodeModels
{
    [DotsSearcherItem(k_Title), Serializable]
    class GetChildAtNodeModel : DotsNodeModel<GetChildAt>, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Get Child At";

        public override string Title => k_Title;
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
