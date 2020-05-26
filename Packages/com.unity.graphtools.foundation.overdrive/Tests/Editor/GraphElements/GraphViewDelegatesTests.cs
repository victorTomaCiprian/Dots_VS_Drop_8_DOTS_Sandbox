using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using Unity.GraphElements;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphViewDelegatesTests : GraphViewTester
    {
        class TestGraphElement : GraphElement
        {
            public TestGraphElement()
            {
                style.backgroundColor = Color.red;
                elementTypeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
        }

        [UnityTest]
        public IEnumerator DeletingGraphElementsExecutesElementDeletedDelegate()
        {
            bool elementDeleted = false;

            graphView.graphViewChanged += graphViewChange =>
            {
                elementDeleted = (graphViewChange.elementsToRemove.Count == 1);
                return graphViewChange;
            };

            Node node1 = CreateNode("", new Rect(50, 50, 50, 50));
            yield return null;

            GraphViewChange change = new GraphViewChange { elementsToRemove = new List<GraphElement> { node1 } };
            window.graphView.graphViewChanged(change);

            yield return null;

            Assert.IsTrue(elementDeleted);

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingGraphElementExecutesElementDraggedDelegate()
        {
            int elementsDraggedCount = 0;

            graphView.graphViewChanged += graphViewChange =>
            {
                elementsDraggedCount = graphViewChange.movedElements.Count;
                return graphViewChange;
            };

            Rect nodePosition = new Rect(50, 50, 50, 50);
            const int nodeCount = 10;
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = CreateNode("Node " + (i + 1), nodePosition);
                nodes.Add(node);
            }
            yield return null;

            var center = nodes.First().worldBound.center;
            var position = nodes.First().worldBound.position;
            var size = nodes.First().worldBound.size;

            var offset = new Vector2(10, 10);

            var startDrag = position - offset;
            var endDrag = position + size - offset;
            var dragOffset = center - offset;

            helpers.MouseDownEvent(startDrag);
            yield return null;

            helpers.MouseDragEvent(startDrag, endDrag);
            yield return null;

            helpers.MouseUpEvent(endDrag);
            yield return null;

            helpers.MouseDownEvent(center);
            yield return null;

            helpers.MouseDragEvent(center, dragOffset);
            yield return null;

            helpers.MouseUpEvent(dragOffset);
            yield return null;

            Assert.AreEqual(elementsDraggedCount, nodeCount);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ResizingGraphElementExecutesElementResizedDelegate()
        {
            bool elementResized = false;

            graphView.elementResized += elements => elementResized = true;

            var graphElement = new TestGraphElement();
            graphElement.SetPosition(new Rect(50, 50, 50, 50));
            graphElement.style.width = 50;
            graphElement.style.height = 50;
            graphElement.capabilities |= Capabilities.Resizable;
            graphView.AddElement(graphElement);
            yield return null;

            var size = graphElement.worldBound.size;
            var resizeElementPosition = graphElement.worldBound.center + size / 2 - new Vector2(5, 15);

            helpers.MouseDownEvent(resizeElementPosition);
            yield return null;

            helpers.MouseDragEvent(resizeElementPosition, resizeElementPosition + new Vector2(10, 10));
            yield return null;

            helpers.MouseUpEvent(resizeElementPosition + new Vector2(10, 10));
            yield return null;

            Assert.IsTrue(elementResized);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangingGraphViewTransformExecutesViewTransformedDelegate()
        {
            bool viewTransformChanged = false;

            graphView.viewTransformChanged += elements => viewTransformChanged = true;

            graphView.UpdateViewTransform(new Vector3(10, 10, 10), new Vector3(10, 10));

            yield return null;
            Assert.IsTrue(viewTransformChanged);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangingZoomLevelExecutesViewTransformedDelegate()
        {
            bool viewTransformChanged = false;
            float minZoomScale = 0.1f;
            float maxZoomScale = 3;

            graphView.viewTransformChanged += elements => viewTransformChanged = true;
            graphView.SetupZoom(minZoomScale, maxZoomScale);
            yield return null;

            helpers.ScrollWheelEvent(10.0f, graphView.worldBound.center);
            yield return null;

            Assert.IsTrue(viewTransformChanged);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangingGraphViewTransformRoundsToPixelGrid()
        {
            var capturedPos = Vector3.zero;
            graphView.viewTransformChanged += graphView => capturedPos = graphView.contentViewContainer.transform.position;

            var pos = new Vector3(10.3f, 10.6f, 10.0f);
            graphView.UpdateViewTransform(pos, new Vector3(10, 10));

            yield return null;
            Assert.AreEqual(new Vector3(GraphViewStaticBridge.RoundToPixelGrid(pos.x), GraphViewStaticBridge.RoundToPixelGrid(pos.y), 10.0f), capturedPos);

            yield return null;
        }
    }
}
