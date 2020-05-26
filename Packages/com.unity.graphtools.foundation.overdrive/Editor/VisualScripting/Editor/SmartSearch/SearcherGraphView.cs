using Unity.GraphElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public sealed class SearcherGraphView : GraphView
    {
        public Store Store { get; }

        public SearcherGraphView(Store store)
        {
            Store = store;

            contentContainer.style.flexBasis = StyleKeyword.Auto;

            AddToClassList("searcherGraphView");
            this.AddStylesheet("SearcherGraphView.uss");
        }
    }
}
