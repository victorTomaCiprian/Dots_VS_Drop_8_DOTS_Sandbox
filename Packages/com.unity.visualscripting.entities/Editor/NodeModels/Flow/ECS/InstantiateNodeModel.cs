using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace NodeModels
{
    [DotsSearcherItem("Instantiate"), Serializable]
    class InstantiateNodeModel : DotsNodeModel<Instantiate>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort,
        IHasMainInputPort, IHasMainOutputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
