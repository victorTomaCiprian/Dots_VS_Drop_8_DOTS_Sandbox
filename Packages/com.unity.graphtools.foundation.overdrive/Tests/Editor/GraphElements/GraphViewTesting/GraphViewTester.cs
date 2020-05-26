using System;
using NUnit.Framework;
using Unity.GraphElements;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphViewTestUtilities
{
    public class GraphViewTester
    {
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.k_PanAreaWidth * 8, SelectionDragger.k_PanAreaWidth * 6));

        protected TestGraphViewWindow window { get; private set; }
        protected TestGraphView graphView { get; private set; }
        protected TestEventHelpers helpers { get; private set; }

        bool m_EnablePersistence;

        public GraphViewTester(bool enablePersistence = false)
        {
            m_EnablePersistence = enablePersistence;
        }

        protected Node CreateNodeWithoutAddingToGraphView(string title, Vector2 pos, int inputs = 0, int outputs = 0, Orientation orientation = Orientation.Horizontal, string viewDataKey = "")
        {
            var node = new Node();

            // Do not override the pre-defined key with an empty string
            // Other tests that do not explicitely test persistence still rely on this for selection behaviour
            if (!string.IsNullOrEmpty(viewDataKey))
            {
                node.viewDataKey = viewDataKey;
            }

            for (int i = 0; i < inputs; ++i)
            {
                var inputPort = node.InstantiatePort(orientation, Direction.Input, Port.Capacity.Single, typeof(float));
                node.inputContainer.Add(inputPort);
            }

            for (int i = 0; i < outputs; ++i)
            {
                var outputPort = node.InstantiatePort(orientation, Direction.Output, Port.Capacity.Multi, typeof(float));
                node.outputContainer.Add(outputPort);
            }

            node.SetPosition(new Rect(pos.x, pos.y, 0, 0));
            node.title = title;
            node.RefreshPorts();

            return node;
        }

        protected Node CreateNode(string title, Vector2 pos, int inputs = 0, int outputs = 0, Orientation orientation = Orientation.Horizontal, string viewDataKey = "")
        {
            var node = CreateNodeWithoutAddingToGraphView(title, pos, inputs, outputs, orientation, viewDataKey);

            graphView.AddElement(node);

            return node;
        }

        protected Node CreateNode(string title, Rect pos, int inputs = 0, int outputs = 0, Orientation orientation = Orientation.Horizontal, string viewDataKey = "")
        {
            var node = CreateNode(title, pos.position, inputs, outputs, orientation, viewDataKey);

            node.style.width = pos.width;
            node.style.height = pos.height;
            return node;
        }

        protected Edge CreateEdge(Port outputPort, Port inputPort)
        {
            var edge = outputPort.ConnectTo(inputPort);
            graphView.AddElement(edge);

            return edge;
        }

        protected Scope CreateScope(float x, float y)
        {
            Scope scope = new Scope();

            scope.SetPosition(new Rect(x, y, 100, 100));

            graphView.AddElement(scope);

            return scope;
        }

        protected Group CreateGroup(string title, float x, float y)
        {
            Group group = new Group();

            group.SetPosition(new Rect(x, y, 100, 100));
            group.title = title;

            graphView.AddElement(group);

            return group;
        }

        protected StackNode CreateStackNode(float x, float y)
        {
            StackNode stackNode = new StackNode();

            stackNode.SetPosition(new Rect(x, y, 100, 100));

            graphView.AddElement(stackNode);

            return stackNode;
        }

        protected void ForceUIUpdate()
        {
            window.RepaintImmediately();
        }

        [SetUp]
        public virtual void SetUp()
        {
            window = EditorWindow.GetWindowWithRect<TestGraphViewWindow>(k_WindowRect);

            if (!m_EnablePersistence)
                GraphViewStaticBridge.DisableViewDataPersistence(window);
            else
                GraphViewStaticBridge.ClearPersistentViewData(window);

            graphView = window.graphView as TestGraphView;
            helpers = new TestEventHelpers(window);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (m_EnablePersistence)
                window.ClearPersistentViewData();

            Clear();
        }

        protected void Clear()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (window != null)
            {
                window.Close();
            }
        }
    }
}
