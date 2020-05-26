using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace NodeModels
{
    [DotsSearcherItem(k_Title), Serializable]
    class GetParentNodeModel : DotsNodeModel<GetParent>, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Get Parent";

        public override string Title => k_Title;
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
