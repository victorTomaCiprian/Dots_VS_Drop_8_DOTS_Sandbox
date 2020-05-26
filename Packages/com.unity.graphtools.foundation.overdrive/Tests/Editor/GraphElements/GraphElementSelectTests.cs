using System.Collections;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphElementSelectTests : GraphViewTester
    {
        private Node m_Node1;
        private Node m_Node2;
        private Node m_Node3;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Rect(10, 30, 50, 50));
            m_Node2 = CreateNode("Node 2", new Rect(70, 30, 50, 50));
            m_Node3 = CreateNode("Node 3", new Rect(100, 30, 50, 50));
        }

        Rect RectAroundNodes()
        {
            // Generate a rectangle to select all the elements
            Rect rectangle = RectUtils.Encompass(RectUtils.Encompass(m_Node1.worldBound, m_Node2.worldBound), m_Node3.worldBound);
            rectangle = RectUtils.Inflate(rectangle, 1, 1, 1, 1);
            return rectangle;
        }

        [UnityTest]
        public IEnumerator ElementCanBeSelected()
        {
            helpers.Click(m_Node1);

            yield return null;

            Assert.True(m_Node1.selected);
            Assert.False(m_Node2.selected);
            Assert.False(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator SelectingNewElementUnselectsPreviousOne()
        {
            // Select elem 1. All other elems should be unselected.
            helpers.Click(m_Node1);

            yield return null;

            Assert.True(m_Node1.selected);
            Assert.False(m_Node2.selected);
            Assert.False(m_Node3.selected);

            // Select elem 2. All other elems should be unselected.
            helpers.Click(m_Node2);

            yield return null;

            Assert.False(m_Node1.selected);
            Assert.True(m_Node2.selected);
            Assert.False(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator SelectionSurvivesNodeRemoval()
        {
            const string key = "node42";
            const string wrongKey = "node43";

            // Create the node.
            Node node = CreateNode(key, new Vector2(200, 200));
            node.viewDataKey = key;

            yield return null;

            // Add to selection.
            graphView.AddToSelection(node);
            Assert.True(node.selected);

            // Remove node.
            graphView.RemoveElement(node);
            Assert.False(node.selected);

            // Add node back and restore selection.
            graphView.AddElement(node);
            Assert.True(node.selected);

            // Remove and add back but with a different viewDataKey.
            graphView.RemoveElement(node);
            node.viewDataKey = wrongKey;
            graphView.AddElement(node);
            Assert.False(node.selected);
        }

        EventModifiers modifiers
        {
            get
            {
                return Application.platform == RuntimePlatform.OSXEditor ? EventModifiers.Command : EventModifiers.Control;
            }
        }

        [UnityTest]
        public IEnumerator SelectingNewElementWithActionAddsToSelection()
        {
            // Select elem 1. All other elems should be unselected.
            helpers.Click(m_Node1);

            yield return null;

            // Select elem 2 with control. 1 and 2 should be selected
            helpers.Click(m_Node2, eventModifiers: modifiers);

            yield return null;

            Assert.True(m_Node1.selected);
            Assert.True(m_Node2.selected);
            Assert.False(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator SelectingSelectedElementWithActionModifierRemovesFromSelection()
        {
            // Select elem 1. All other elems should be unselected.
            helpers.Click(m_Node1);

            yield return null;

            // Select elem 2 with control. 1 and 2 should be selected
            helpers.Click(m_Node2, eventModifiers: modifiers);

            yield return null;

            // Select elem 1 with control. Only 2 should be selected
            helpers.Click(m_Node1, eventModifiers: modifiers);

            yield return null;

            Assert.False(m_Node1.selected);
            Assert.True(m_Node2.selected);
            Assert.False(m_Node3.selected);
        }

        // Taken from internal QuadTree utility
        static bool Intersection(Rect r1, Rect r2, out Rect intersection)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
            {
                intersection = new Rect(0, 0, 0, 0);
                return false;
            }

            float left = Mathf.Max(r1.xMin, r2.xMin);
            float top = Mathf.Max(r1.yMin, r2.yMin);

            float right = Mathf.Min(r1.xMax, r2.xMax);
            float bottom = Mathf.Min(r1.yMax, r2.yMax);
            intersection = new Rect(left, top, right - left, bottom - top);
            return true;
        }

        [UnityTest]
        public IEnumerator ClickOnTwoOverlappingElementsSelectsTopOne()
        {
            // Find the intersection between those two nodes and click right in the middle
            Rect intersection;
            Assert.IsTrue(Intersection(m_Node2.worldBound, m_Node3.worldBound, out intersection), "Expected rectangles to intersect");

            helpers.Click(intersection.center);

            yield return null;

            Assert.False(m_Node1.selected);
            Assert.False(m_Node2.selected);
            Assert.True(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator RectangleSelectionWorks()
        {
            Rect rectangle = RectAroundNodes();

            helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.True(m_Node1.selected);
            Assert.True(m_Node2.selected);
            Assert.True(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator RectangleSelectionWithActionKeyWorks()
        {
            graphView.AddToSelection(m_Node1);
            Assert.True(m_Node1.selected);
            Assert.False(m_Node2.selected);
            Assert.False(m_Node3.selected);

            Rect rectangle = RectAroundNodes();

            // Reselect all.
            helpers.DragTo(rectangle.min, rectangle.max, eventModifiers: modifiers);

            yield return null;

            Assert.False(m_Node1.selected);
            Assert.True(m_Node2.selected);
            Assert.True(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator FreehandSelectionWorks()
        {
            Rect rectangle = RectAroundNodes();

            float lineAcrossNodes = rectangle.y + (rectangle.yMax - rectangle.y) * 0.5f;
            Vector2 startPoint = new Vector2(rectangle.xMax, lineAcrossNodes);
            Vector2 endPoint = new Vector2(rectangle.xMin, lineAcrossNodes);
            helpers.DragTo(startPoint, endPoint, eventModifiers: EventModifiers.Shift, steps: 10);

            yield return null;

            Assert.True(m_Node1.selected);
            Assert.True(m_Node2.selected);
            Assert.True(m_Node3.selected);
        }

        [UnityTest]
        public IEnumerator FreehandDeleteWorks()
        {
            m_Node1.capabilities |= Capabilities.Deletable;
            m_Node2.capabilities |= Capabilities.Deletable;
            m_Node3.capabilities |= Capabilities.Deletable;

            Rect rectangle = RectAroundNodes();

            float lineAcrossNodes = rectangle.y + (rectangle.yMax - rectangle.y) * 0.5f;
            Vector2 startPoint = new Vector2(rectangle.xMax, lineAcrossNodes);
            Vector2 endPoint = new Vector2(rectangle.xMin, lineAcrossNodes);
            helpers.DragTo(startPoint, endPoint, eventModifiers: EventModifiers.Shift | EventModifiers.Alt, steps: 10);

            yield return null;

            // After manipulation we should have only zero elements left.
            Assert.AreEqual(0, graphView.graphElements.ToList().Count);
        }

        [Test]
        public void AddingElementToSelectionTwiceDoesNotAddTheSecondTime()
        {
            Assert.AreEqual(0, graphView.selection.Count);

            graphView.AddToSelection(m_Node1);
            Assert.AreEqual(1, graphView.selection.Count);

            // Add same element again, should have no impact on selection
            graphView.AddToSelection(m_Node1);
            Assert.AreEqual(1, graphView.selection.Count);

            // Add other element
            graphView.AddToSelection(m_Node2);
            Assert.AreEqual(2, graphView.selection.Count);
        }

        [Test]
        public void RemovingElementFromSelectionTwiceDoesThrowException()
        {
            graphView.AddToSelection(m_Node1);
            graphView.AddToSelection(m_Node2);
            Assert.AreEqual(2, graphView.selection.Count);

            graphView.RemoveFromSelection(m_Node2);
            Assert.AreEqual(1, graphView.selection.Count);

            // Remove the same item again, should have no impact on selection
            graphView.RemoveFromSelection(m_Node2);
            Assert.AreEqual(1, graphView.selection.Count);

            // Remove other element
            graphView.RemoveFromSelection(m_Node1);
            Assert.AreEqual(0, graphView.selection.Count);
        }
    }
}
