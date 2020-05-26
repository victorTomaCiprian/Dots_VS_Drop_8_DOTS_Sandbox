using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace NodeModels
{
    [Serializable, EnumNodeSearcher(typeof(InterpolationType), "Math", "{0} Interpolation")]
    class InterpolateNodeModel : DotsNodeModel<Interpolate>, IHasMainInputPort, IHasMainOutputPort
    {
        public override string Title => $"{TypedNode.Type.ToString().Nicify()} Interpolation";
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
