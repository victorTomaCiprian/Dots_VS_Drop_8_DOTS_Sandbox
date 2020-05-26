using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphGroupTests : GraphScopeTestsBase
    {
        static readonly Vector2 k_OutsidePosition = new Vector2(-5, -5);

        [Test]
        public void GroupStartsEmpty()
        {
            // Create a group
            Group group = CreateGroup("Group", 10, 10);

            // Verify that it is empty
            Assert.AreEqual(0, group.containedElements.Count());
        }

        [Test]
        public void PreventNestedGroup()
        {
            // Creates two groups
            Group group1 = CreateGroup("Group 1", 10, 10);
            Group group2 = CreateGroup("Group 2", 10, 10);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => { group1.AddElement(group2); });

            Assert.AreEqual("Nested group is not supported yet.", ex.Message);
        }

        [UnityTest]
        public IEnumerator DragAddSelectionToGroup()
        {
            // Create a group
            Group group = CreateGroup("Group", 10, 10);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Select node1 and node2
            m_Node1.Select(graphView, false);
            m_Node2.Select(graphView, true);

            // Drag selected nodes (starting from one of the nodes)
            Vector2 start = m_Node1.worldBound.center;
            Vector2 centerOfGroupContentArea = group.LocalToWorld(group.containedElementsRect.center);

            // Drag node1 and node2 onto the content area of the group
            helpers.DragTo(start, centerOfGroupContentArea);

            // Verify that both nodes have been added to group
            Assert.AreEqual(2, group.containedElements.Count());
            Assert.IsTrue(group.ContainsElement(m_Node1));
            Assert.IsTrue(group.ContainsElement(m_Node2));
        }

        [UnityTest]
        public IEnumerator DragRemoveSelectionFromGroup()
        {
            // Creates a group and add node1, node2, node3
            Group group = CreateGroup("Group", 10, 10);

            group.AddElement(m_Node1);
            group.AddElement(m_Node2);
            group.AddElement(m_Node3);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Select node1 and node2
            m_Node1.Select(graphView, false);
            m_Node2.Select(graphView, true);

            // Drag selected nodes (starting from one of the nodes)
            Vector2 start = m_Node1.worldBound.center;
            Vector2 outsideGroup = group.LocalToWorld(k_OutsidePosition);

            // Drag node1 and node2 away from the group holding Shift key
            helpers.MouseDownEvent(start, MouseButton.LeftMouse, EventModifiers.Shift);

            // Needed in order for the group to receive a drag event
            helpers.MouseDragEvent(start, start + Vector2.one, MouseButton.LeftMouse, EventModifiers.Shift);

            helpers.MouseDragEvent(start, outsideGroup, MouseButton.LeftMouse, EventModifiers.Shift);

            helpers.MouseUpEvent(outsideGroup);

            Assert.AreEqual(1, group.containedElements.Count());
            Assert.IsTrue(group.ContainsElement(m_Node3));
        }

        [UnityTest]
        public IEnumerator DragPreventFromStealing()
        {
            // Creates two groups
            Group group1 = CreateGroup("Group 1", 10, 10);
            Group group2 = CreateGroup("Group 2", 400, 400);

            group1.AddElement(m_Node1);

            // Select node1
            m_Node1.Select(graphView, false);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            Vector2 start = m_Node1.LocalToWorld(Vector2.zero);

            Vector2 centerOfGroup2ContentArea = group2.LocalToWorld(group2.containedElementsRect.center);

            // Drag node1 onto the content area of the group2.
            helpers.DragTo(start, centerOfGroup2ContentArea);

            // Ensure that node1 was not stolen by group2.
            Assert.AreEqual(group1, m_Node1.GetContainingScope());
        }
    }
}
