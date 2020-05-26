using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [DotsSearcherItem(k_Title), Serializable]
    class MakeFloat3NodeModel : DotsNodeModel<MakeFloat3>, IHasMainOutputPort
    {
        const string k_Title = "Make Vector 3";

        public override string Title => k_Title;

        public IPortModel OutputPort { get; set; }
    }
}
