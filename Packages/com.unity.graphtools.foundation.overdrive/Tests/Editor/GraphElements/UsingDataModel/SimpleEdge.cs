using UnityEngine;
using Unity.GraphElements;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleEdge : Edge
    {
        public SimpleEdge()
        {
            style.overflow = Overflow.Visible;
        }
    }
}
