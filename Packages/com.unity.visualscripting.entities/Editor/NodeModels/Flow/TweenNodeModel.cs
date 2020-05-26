using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace NodeModels
{
    [Serializable, EnumNodeSearcher(typeof(InterpolationType), "Flow", "{0} Tween")]
    class TweenNodeModel : DotsNodeModel<Tween>, IHasMainInputPort, IHasMainOutputPort,
        IHasMainExecutionInputPort, IHasMainExecutionOutputPort
    {
        public override string Title => $"{TypedNode.Type.ToString().Nicify()} Tween";
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
    }
}
