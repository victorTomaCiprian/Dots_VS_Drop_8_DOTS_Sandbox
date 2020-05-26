using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Flow/" + k_Title)]
    class OnChangeNodeModel : DotsNodeModel<OnChange>, IHasMainInputPort, IHasMainExecutionInputPort, IHasMainExecutionOutputPort
    {
        const string k_Title = "On Change";

        public override string Title => k_Title;

        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
