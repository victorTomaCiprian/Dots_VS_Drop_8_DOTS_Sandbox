using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace NodeModels
{
    [DotsSearcherItem(k_Title), Serializable]
    class GetChildrenCountNodeModel : DotsNodeModel<GetChildrenCount>, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Get Children Count";

        public override string Title => k_Title;
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
