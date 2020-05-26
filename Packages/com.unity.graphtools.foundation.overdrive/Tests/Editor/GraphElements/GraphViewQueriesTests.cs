using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphViewQueriesTests : GraphViewTester
    {
        Node m_Node1;
        Node m_Node2;
        Node m_Node3;
        Node m_Node4;
        Group m_Group;
        StackNode m_StackNode;
        Edge m_Edge1;
        Edge m_Edge2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(100, 100), 2, 2);
            m_Node2 = CreateNode("Node 2", new Vector2(200, 200), 2, 2);
            m_Node3 = CreateNode("Node 3", new Vector2(400, 400));
            m_Node4 = CreateNode("Node 4", new Vector2(500, 500));
            m_Group = CreateGroup("Group", 500, 0);
            m_StackNode = CreateStackNode(300, 300);
            m_Edge1 = CreateEdge(m_Node2.outputContainer[0] as Port, m_Node1.inputContainer[0] as Port);
            m_Edge2 = CreateEdge(m_Node2.outputContainer[1] as Port, m_Node1.inputContainer[0] as Port);

            m_Group.AddElement(m_Node2);

            m_StackNode.AddElement(m_Node3);
            m_StackNode.AddElement(m_Node4);
        }

        [Test]
        public void QueryAllElements()
        {
            List<GraphElement> allElements = graphView.graphElements.ToList();

            Assert.AreEqual(8, allElements.Count);
            Assert.IsFalse(allElements.OfType<Port>().Any());
            Assert.IsTrue(allElements.Contains(m_Node1));
            Assert.IsTrue(allElements.Contains(m_Node2));
            Assert.IsTrue(allElements.Contains(m_Node3));
            Assert.IsTrue(allElements.Contains(m_Node4));
            Assert.IsTrue(allElements.Contains(m_Group));
            Assert.IsTrue(allElements.Contains(m_StackNode));
            Assert.IsTrue(allElements.Contains(m_Edge1));
            Assert.IsTrue(allElements.Contains(m_Edge2));
        }

        [Test]
        public void QueryAllNodes()
        {
            List<Node> allNodes = graphView.nodes.ToList();

            Assert.AreEqual(5, allNodes.Count);
            Assert.IsTrue(allNodes.Contains(m_Node1));
            Assert.IsTrue(allNodes.Contains(m_Node2));
            Assert.IsTrue(allNodes.Contains(m_Node3));
            Assert.IsTrue(allNodes.Contains(m_Node4));
            Assert.IsTrue(allNodes.Contains(m_StackNode));
        }

        [Test]
        public void QueryAllEdges()
        {
            List<Edge> allEdges = graphView.edges.ToList();

            Assert.AreEqual(2, allEdges.Count);
            Assert.IsTrue(allEdges.Contains(m_Edge1));
            Assert.IsTrue(allEdges.Contains(m_Edge2));
        }

        [Test]
        public void QueryAllPorts()
        {
            Assert.AreEqual(8, graphView.ports.ToList().Count);
        }
    }
}
