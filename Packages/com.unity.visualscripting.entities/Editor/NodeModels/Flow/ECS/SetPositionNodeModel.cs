using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class SetPositionNodeModel : DotsNodeModel<SetPosition>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort,
        IHasMainInputPort
    {
        const string k_Title = "Set Position";

        public override string Title => k_Title;

        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
