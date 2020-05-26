using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class RotationEulerNodeModel : DotsNodeModel<RotationEuler>, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Rotation Euler";

        public override string Title => k_Title;
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
