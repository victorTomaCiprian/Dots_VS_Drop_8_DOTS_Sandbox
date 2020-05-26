using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace DotsStencil
{
    [Serializable, EnumNodeSearcher(typeof(Time.TimeType), "Time")]
    class TimeNodeModel : DotsNodeModel<Time>, IHasMainOutputPort
    {
        public override string Title => TypedNode.Type.ToString().Nicify();
        public IPortModel OutputPort { get; set; }
    }
}
