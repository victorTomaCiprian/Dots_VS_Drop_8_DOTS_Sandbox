using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace DotsStencil
{
    [Serializable, EnumNodeSearcher(typeof(OnKey.KeyEventType), "Events", "On Key {0}")]
    class OnKeyDownNodeModel : DotsNodeModel<OnKey>, IHasMainExecutionOutputPort
    {
        const string k_Title = "On Key";

        public override string Title => k_Title + " " + TypedNode.EventType.ToString().Nicify();

        public IPortModel ExecutionOutputPort { get; set; }
    }
}
