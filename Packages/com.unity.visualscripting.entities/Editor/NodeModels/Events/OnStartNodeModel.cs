using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Events/" + k_Title)]
    class OnStartNodeModel : DotsNodeModel<OnStart>, IHasMainExecutionOutputPort
    {
        const string k_Title = "On Start";

        public override string Title => k_Title;

        public IPortModel ExecutionOutputPort { get; set; }
    }
}
