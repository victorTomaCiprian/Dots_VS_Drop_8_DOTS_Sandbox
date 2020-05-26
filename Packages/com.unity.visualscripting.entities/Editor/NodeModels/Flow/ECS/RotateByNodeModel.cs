using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem(k_Title)]
    class RotateByNodeModel : DotsNodeModel<RotateBy>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort,
        IHasMainInputPort
    {
        const string k_Title = "Rotate By";

        public override string Title => k_Title;

        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
    }
}
