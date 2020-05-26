using System;
using Unity.GraphElements;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class HighLevelNode : Node
    {
        public HighLevelNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView) {}

        protected override void UpdateFromModel()
        {
            base.UpdateFromModel();

            AddToClassList("highLevelNode");

            VisualElement topHorizontalDivider = this.MandatoryQ("divider", "horizontal");
            VisualElement topVerticalDivider = this.MandatoryQ("divider", "vertical");

            // GraphView automatically hides divider since there are no input ports
            topHorizontalDivider.RemoveFromClassList("hidden");
            topVerticalDivider.RemoveFromClassList("hidden");

            VisualElement output = this.MandatoryQ("output");
            output.AddToClassList("node-controls");
        }
    }
}
