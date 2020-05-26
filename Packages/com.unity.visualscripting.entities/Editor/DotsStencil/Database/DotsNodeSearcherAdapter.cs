using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using UnityEditor;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace DotsStencil
{
    public class DotsNodeSearcherAdapter : GraphNodeSearcherAdapter
    {
        VisualElement m_Description;

        public DotsNodeSearcherAdapter(IGraphModel graphModel, string title)
            : base(graphModel, title) {}

        /// <inheritdoc />
        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            base.InitDetailsPanel(detailsPanel);
            detailsPanel.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIHelper.TemplatePath + "DotsSearcher.uss"));
        }

        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            m_Description?.Clear();

            base.OnSelectionChanged(items);
        }

        protected override void OnGraphElementsCreated(SearcherItem searcherItem,
            IEnumerable<IGraphElementModel> elements)
        {
            if (m_DetailsPanel == null)
                return;

            if (m_Description == null)
            {
                m_Description = new VisualElement { name = "nodeDescription" };
                m_Scrollview.Add(m_Description);
            }

            var elementList = elements.ToList();
            if (!elementList.Any() || !(elementList.First() is IDotsNodeModel baseDotsNodeModel))
                return;

            m_Description.Clear();
            NodeDocumentationFormatter formatter = new SearcherNodeDocumentationFormatter(m_Description);
            formatter.DocumentNode(searcherItem, baseDotsNodeModel);
        }
    }
}
