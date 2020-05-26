using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [DotsSearcherItem("Random Int"), Serializable]
    class RandomIntNodeModel : DotsNodeModel<RandomInt>, IHasMainOutputPort
    {
        public IPortModel OutputPort { get; set; }
    }
}
