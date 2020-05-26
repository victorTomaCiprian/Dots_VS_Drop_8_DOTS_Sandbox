using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Math/Combine Quaternion Rotations")]
    class MathQuaternionNodeModel : DotsNodeModel<CombineQuaternionRotations>, IHasMainOutputPort
    {
        public override string Title => "Combine Quaternion Rotations";
        public IPortModel OutputPort { get; set; }
    }
}
