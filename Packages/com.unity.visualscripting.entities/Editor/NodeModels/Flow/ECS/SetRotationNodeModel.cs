using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class SetRotationNodeModel : DotsNodeModel<SetRotation>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort,
        IHasMainInputPort
    {
        const string k_Title = "Set Rotation";

        public override string Title => k_Title;

        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
