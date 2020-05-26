using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [DotsSearcherItem("Random Float"), Serializable]
    class RandomNodeModel : DotsNodeModel<RandomFloat>, IHasMainOutputPort
    {
        public IPortModel OutputPort { get; set; }
    }
}
