using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [DotsSearcherItem(k_Title), Serializable]
    class MathUnaryNodeBool : DotsNodeModel<MathUnaryNotBool>, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Not";

        public override string Title => k_Title;

        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
