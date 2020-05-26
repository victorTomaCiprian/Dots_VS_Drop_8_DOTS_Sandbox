using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Flow/" + k_Title)]
    class StateSwitchNodeModel : DotsNodeModel<StateSwitch>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort, IHasMainOutputPort
    {
        const string k_Title = "State Switch";
        public override string Title => k_Title;
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
