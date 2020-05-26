using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace DotsStencil
{
    [Serializable, EnumNodeSearcher(typeof(MathBinaryNumberBool.BinaryNumberType), "Flow")]
    class MathBinaryNumberBoolNodeModel : DotsNodeModel<MathBinaryNumberBool>, IHasMainInputPort, IHasMainOutputPort
    {
        public override string Title => TypedNode.Type.ToString().Nicify();
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
