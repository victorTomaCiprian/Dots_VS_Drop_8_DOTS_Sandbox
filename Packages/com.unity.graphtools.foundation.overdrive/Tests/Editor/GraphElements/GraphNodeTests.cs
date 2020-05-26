using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphElements;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphNodeTests : GraphViewTester
    {
        private Node m_Node1;
        private Node m_Node2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = new Node();
            m_Node1.SetPosition(new Rect(0, 0, 200, 200));
            m_Node1.style.width = 200;
            m_Node1.style.height = 200;
            m_Node1.title = "Node 1";
            graphView.AddElement(m_Node1);
            var outputPort = m_Node1.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            m_Node1.outputContainer.Add(outputPort);
            m_Node1.RefreshPorts();

            m_Node2 = new Node();
            m_Node2.SetPosition(new Rect(300, 300, 200, 200));
            m_Node2.style.width = 200;
            m_Node2.style.height = 200;
            m_Node2.title = "Node 2";
            graphView.AddElement(m_Node2);
            var inputPort = m_Node2.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            m_Node2.inputContainer.Add(inputPort);
            m_Node2.RefreshPorts();

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.SetPosition(new Rect(10, 100, 100, 100));
            miniMap.maxWidth = 100;
            miniMap.maxHeight = 100;
            graphView.Add(miniMap);
        }

        [UnityTest]
        public IEnumerator CollapseButtonOnlyEnabledWhenNodeHasUnconnectedPorts()
        {
            List<Port> ports = graphView.ports.ToList();
            List<Node> nodeList = graphView.nodes.ToList();

            // Nothing is connected. The collapse button should be enabled.
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.Q<VisualElement>(name: "collapse-button");
                Assert.False(collapseButton.GetDisabledPseudoState());
            }

            var edge = new Edge();
            edge.output = m_Node1.outputContainer[0] as Port;
            edge.input = m_Node2.inputContainer[0] as Port;
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            graphView.AddElement(edge);
            yield return null;

            // Ports are connected. The collapse button should be disabled.
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.Q<VisualElement>(name: "collapse-button");
                Assert.True(collapseButton.GetDisabledPseudoState());
            }

            // Disconnect the ports of the 2 nodes.
            ports[0].Disconnect(edge);
            ports[1].Disconnect(edge);
            graphView.RemoveElement(edge);
            yield return null;

            // Once more, nothing is connected. The collapse button should be enabled.
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.Q<VisualElement>(name: "collapse-button");
                Assert.False(collapseButton.GetDisabledPseudoState());
            }
        }

        [UnityTest]
        public IEnumerator SelectedNodeCanBeDeleted()
        {
            int initialCount = graphView.nodes.ToList().Count;
            Assert.Greater(initialCount, 0);

            Node node = graphView.nodes.First();
            graphView.AddToSelection(node);
            graphView.DeleteSelection();
            yield return null;

            Assert.AreEqual(initialCount - 1, graphView.nodes.ToList().Count);
        }

        [UnityTest]
        public IEnumerator SelectedEdgeCanBeDeleted()
        {
            // Connect the ports of the 2 nodes.
            var edge = new Edge();
            edge.output = m_Node1.outputContainer[0] as Port;
            edge.input = m_Node2.inputContainer[0] as Port;
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            graphView.AddElement(edge);
            yield return null;

            int initialCount = window.graphView.edges.ToList().Count;
            Assert.Greater(initialCount, 0);

            window.graphView.AddToSelection(edge);
            window.graphView.DeleteSelection();
            yield return null;

            Assert.AreEqual(initialCount - 1, window.graphView.edges.ToList().Count);
        }

        [UnityTest]
        public IEnumerator EdgeColorsMatchCustomPortColors()
        {
            var edge = new Edge();
            var outputPort = m_Node1.outputContainer[0] as Port;
            var inputPort = m_Node2.inputContainer[0] as Port;
            edge.output = outputPort;
            edge.input = inputPort;
            inputPort.portColor = Color.black;
            outputPort.portColor = Color.white;
            inputPort.Connect(edge);
            outputPort.Connect(edge);
            graphView.AddElement(edge);
            yield return null;

            var edgeControl = edge.edgeControl;
            Assert.AreEqual(inputPort.portColor, edgeControl.inputColor);
            Assert.AreEqual(outputPort.portColor, edgeControl.outputColor);
        }
    }

    internal static class GraphNodeTestsAdapters
    {
        internal static bool Adapt(this NodeAdapter value, PortSource<float> a, PortSource<float> b)
        {
            // run adapt code for float to float connections
            return true;
        }
    }
}
