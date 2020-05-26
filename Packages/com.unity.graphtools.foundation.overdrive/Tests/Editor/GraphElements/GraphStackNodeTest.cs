using System;
using System.Collections;
using NUnit.Framework;
using Unity.GraphElements;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphStackNodeTests : GraphViewTester
    {
        static readonly float k_DefaultX = 250;
        static readonly float k_DefaultY = 250;
        static readonly Vector2 k_OutsidePosition = new Vector2(-5, -5);
        Node m_Node1;
        Node m_Node2;
        Node m_Node3;

        class UnsupportedNode : Node
        {
        }

        class FooterNode : Node
        {
        }

        class FilteredStackNode : StackNode
        {
            public FilteredStackNode()
            {
            }

            protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
            {
                if (element is UnsupportedNode)
                    return false;

                if (element is FooterNode)
                    proposedIndex = maxIndex;

                return true;
            }
        }

        StackNode CreateFilteredStackNode(float x, float y)
        {
            StackNode stackNode = new FilteredStackNode();

            stackNode.SetPosition(new Rect(x, y, 100, 100));

            graphView.AddElement(stackNode);

            return stackNode;
        }

        Node CreateFooterNode(float x, float y)
        {
            Node node = new FooterNode();

            node.SetPosition(new Rect(x, y, 100, 100));

            graphView.AddElement(node);

            return node;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("First Node", new Vector2(50, 50), 0, 3);
            m_Node2 = CreateNode("Second Node", new Vector2(250, 50), 3, 0);
            m_Node3 = CreateNode("Third Node", new Vector2(50, 250), 0, 2);
        }

        [Test]
        public void StackNodeStartsEmpty()
        {
            // Create a stack
            StackNode stack = CreateStackNode(k_DefaultX, k_DefaultY);

            // Verify that it is empty
            Assert.AreEqual(0, stack.childCount);
        }

        [Test]
        public void ValidateSupportedElements()
        {
            StackNode stack = CreateFilteredStackNode(k_DefaultX, k_DefaultY);

            stack.AddElement(CreateStackNode(10, 10));
            stack.AddElement(CreateScope(10, 10));
            stack.AddElement(CreateGroup("", 10, 10));
            stack.AddElement(new UnsupportedNode());
            // Verify that none of the above attempts succeeded
            Assert.AreEqual(0, stack.childCount);

            stack.AddElement(m_Node1);
            //Verify that a regular element is accepted
            Assert.AreEqual(1, stack.childCount);
        }

        [Test]
        public void StackNodeContainsAddedElements()
        {
            // Create a stack
            StackNode stack = CreateFilteredStackNode(k_DefaultX, k_DefaultY);
            Node footer;

            // Verify that nodes are properly added to the stack
            stack.AddElement(m_Node1);
            stack.AddElement(m_Node2);
            stack.InsertElement(0, m_Node3);
            stack.InsertElement(0, footer = new FooterNode());

            Assert.AreEqual(4, stack.childCount);
            Assert.AreEqual(stack, m_Node1.parent);
            Assert.AreEqual(stack, m_Node2.parent);
            Assert.AreEqual(stack, m_Node3.parent);
            Assert.AreEqual(stack, footer.parent);
            Assert.AreEqual(0, stack.IndexOf(m_Node3));
            // Verify that the footer is added at the end of the stack even though we tried to insert it at the begining
            Assert.AreEqual(stack.childCount - 1, stack.IndexOf(footer));
        }

        [Test]
        public void StackNodeDoesNoContainRemovedElements()
        {
            // Create a stack
            StackNode stack = CreateStackNode(k_DefaultX, k_DefaultY);

            stack.AddElement(m_Node1);
            stack.AddElement(m_Node2);

            // Verify that a node is properly removed from its containing stack
            stack.RemoveElement(m_Node1);

            Assert.AreEqual(1, stack.childCount);
            Assert.IsFalse(stack.Contains(m_Node1));
            Assert.IsNull(m_Node1.parent);

            stack.RemoveElement(m_Node2);

            Assert.AreEqual(0, stack.childCount);
            Assert.IsFalse(stack.Contains(m_Node2));
            Assert.IsNull(m_Node2.parent);
        }

        [UnityTest]
        public IEnumerator DragAddSelectionToStackNode()
        {
            // Create a stack
            StackNode stack = CreateStackNode(k_DefaultX, k_DefaultY);

            // Allow two frames to compute the layout of the stack and nodes
            yield return null;
            yield return null;

            // Select node1
            m_Node1.Select(graphView, false);

            Vector2 start = m_Node1.worldBound.center;
            Vector2 centerOfStack = stack.LocalToWorld(stack.GetRect().center); // Center of the stack

            // Drag node1 onto the stack
            helpers.DragTo(start, centerOfStack);

            // Verify that node1 has been added to stack
            Assert.AreEqual(1, stack.childCount);
            Assert.AreEqual(stack, m_Node1.parent);

            // Allow one frame to compute the layout of the stack
            yield return null;

            // Select node2
            m_Node2.Select(graphView, false);

            start = m_Node2.worldBound.center;
            Vector2 centerOfNode1 = m_Node1.worldBound.center; // Center of Node1

            // Drag node2 onto the stack at the position of node1
            helpers.DragTo(start, centerOfNode1);

            // Verify that node2 has been added to the stack at the previous index of node1
            Assert.AreEqual(2, stack.childCount);
            Assert.AreEqual(stack, m_Node2.parent);
            Assert.AreEqual(0, stack.IndexOf(m_Node2));
        }

        [UnityTest]
        public IEnumerator DragReorderSelectionInStackNode()
        {
            // Creates a stack and add node1, node2, node3
            StackNode stack = CreateStackNode(k_DefaultX, k_DefaultY);

            stack.AddElement(m_Node1);
            stack.AddElement(m_Node2);
            stack.AddElement(m_Node3);

            // Allow two frames to compute the layout of the stack and nodes
            yield return null;
            yield return null;

            // Select node1
            m_Node1.Select(graphView, false);

            Vector2 start = m_Node1.worldBound.center;
            Vector2 outsideStackNode = stack.LocalToWorld(k_OutsidePosition);

            // Drag node1 away from the stack
            helpers.MouseDownEvent(start, MouseButton.LeftMouse);
            helpers.MouseDragEvent(start, outsideStackNode, MouseButton.LeftMouse);

            // Allow one frame to compute the layout of the stack
            yield return null;

            Vector2 centerOfNode3 = m_Node3.worldBound.center;

            helpers.MouseDragEvent(outsideStackNode, centerOfNode3, MouseButton.LeftMouse);
            helpers.MouseUpEvent(centerOfNode3);

            // Verify that node1 has been moved to index 1 (before node3)
            Assert.AreEqual(3, stack.childCount);
            Assert.AreEqual(1, stack.IndexOf(m_Node1));
        }

        [UnityTest] // Case 1015984
        public IEnumerator DragRemoveAndThenAddSelectionBackToStackNode()
        {
            // Creates a stack and add node1
            StackNode stack = CreateStackNode(k_DefaultX, k_DefaultY);

            stack.AddElement(m_Node1);

            // Allow two frames to compute the layout of the stack and nodes
            yield return null;
            yield return null;

            // Select node1
            m_Node1.Select(graphView, false);

            Vector2 start = m_Node1.worldBound.center;
            Vector2 outsideStackNode = stack.LocalToWorld(k_OutsidePosition);

            // Drag node1 away from the stack
            helpers.DragTo(start, outsideStackNode);

            Assert.AreEqual(0, stack.childCount);

            // Allow one frame to compute the layout of the stack and node1
            yield return null;

            start = m_Node1.worldBound.center;
            Vector2 centerOfStack = stack.LocalToWorld(stack.GetRect().center); // Center of the stack

            // Drag node1 onto the stack
            helpers.DragTo(start, centerOfStack);

            // Verify that node1 has been added back to the stack
            Assert.AreEqual(1, stack.childCount);
            Assert.AreEqual(stack, m_Node1.parent);
        }

        [UnityTest]
        public IEnumerator DragRemoveSelectionFromStackNode()
        {
            // Creates a stack and add node1, node2, node3
            StackNode stack = CreateStackNode(k_DefaultX, k_DefaultY);

            stack.AddElement(m_Node1);
            stack.AddElement(m_Node2);
            stack.AddElement(m_Node3);

            // Allow two frames to compute the layout of the stack and nodes
            yield return null;
            yield return null;

            // Select node1 and node2
            m_Node1.Select(graphView, false);
            m_Node2.Select(graphView, true);

            Vector2 start = m_Node1.worldBound.center;
            Vector2 outsideStackNode = stack.LocalToWorld(k_OutsidePosition);

            // Drag node1 and node2 away from the stack
            helpers.DragTo(start, outsideStackNode);

            Assert.AreEqual(1, stack.childCount);
            Assert.IsTrue(stack.Contains(m_Node3));
        }
    }
}
