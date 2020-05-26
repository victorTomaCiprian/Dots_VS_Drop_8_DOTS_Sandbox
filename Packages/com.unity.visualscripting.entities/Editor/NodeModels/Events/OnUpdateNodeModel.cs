using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Events/" + k_Title)]
    class OnUpdateNodeModel : DotsNodeModel<OnUpdate>, IHasMainExecutionOutputPort
    {
        const string k_Title = "On Update";

        public override string Title => k_Title;

        public IPortModel ExecutionOutputPort { get; set; }
    }
}
