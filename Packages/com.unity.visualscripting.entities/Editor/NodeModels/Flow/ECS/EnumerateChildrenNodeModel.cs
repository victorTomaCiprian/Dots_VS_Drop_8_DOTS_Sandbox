using System;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace NodeModels
{
    [DotsSearcherItem(k_Title), Serializable]
    class EnumerateChildrenNodeModel : DotsNodeModel<EnumerateChildren>, IHasMainExecutionInputPort,
        IHasMainExecutionOutputPort, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Enumerate Children";

        public override string Title => k_Title;
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }
    }
}
