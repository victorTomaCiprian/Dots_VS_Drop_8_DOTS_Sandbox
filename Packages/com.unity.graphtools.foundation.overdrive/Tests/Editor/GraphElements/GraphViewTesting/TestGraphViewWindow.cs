using System;
using UnityEngine.UIElements;
using Unity.GraphElements;
using Unity.GraphToolsFoundations.Bridge;

namespace UnityEditor.GraphViewTestUtilities
{
    public class TestGraphViewWindow : EditorWindow
    {
        public GraphView graphView { get; private set; }

        public TestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
        }

        public void OnEnable()
        {
            graphView = new TestGraphView();

            graphView.name = "theView";
            graphView.viewDataKey = "theView";
            graphView.StretchToParentSize();

            rootVisualElement.Add(graphView);
        }

        public void OnDisable()
        {
            rootVisualElement.Remove(graphView);
        }
    }
}
