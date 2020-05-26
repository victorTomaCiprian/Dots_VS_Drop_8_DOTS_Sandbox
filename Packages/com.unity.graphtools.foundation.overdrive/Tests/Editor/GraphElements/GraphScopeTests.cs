using System;
using System.Linq;
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
    public class GraphScopeTestsBase : GraphViewTester
    {
        protected const int k_MaxIterations = 5;
        protected Node m_Node1;
        protected Node m_Node2;
        protected Node m_Node3;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("First Node", new Vector2(50, 50), 0, 3);
            m_Node2 = CreateNode("Second Node", new Vector2(250, 50), 3, 0);
            m_Node3 = CreateNode("Third Node", new Vector2(50, 250), 0, 2);
        }
    }

    public class GraphScopeTests : GraphScopeTestsBase
    {
        private static void GetScopeMargins(Scope scope, out float left, out float top, out float right, out float bottom)
        {
            Rect scopeRect = scope.GetRect();
            Rect containedElementsRect = scope.containedElementsRect;

            left = containedElementsRect.xMin - scopeRect.xMin;
            top = containedElementsRect.yMin - scopeRect.yMin;
            right = scopeRect.xMax - containedElementsRect.xMax;
            bottom = scopeRect.yMax - containedElementsRect.yMax;
        }

        private Rect GetBoundingRectInViewportSpace(IEnumerable<GraphElement> elements)
        {
            VisualElement viewport = graphView.contentViewContainer;
            Rect contentRectInViewportpace = Rect.zero;

            int index = 0;

            foreach (GraphElement subElement in elements)
            {
                Rect boundingRect = new Rect(0, 0, subElement.GetPosition().width, subElement.GetPosition().height);

                boundingRect = subElement.ChangeCoordinatesTo(viewport, boundingRect);

                // Use the first element as reference to compute the bounding box of contained elements
                if (index == 0)
                {
                    contentRectInViewportpace = boundingRect;
                }
                else
                {
                    contentRectInViewportpace = RectUtils.Encompass(contentRectInViewportpace, boundingRect);
                }
                index++;
            }

            return contentRectInViewportpace;
        }

        [Test]
        public void ScopeStartsEmpty()
        {
            // Create a scope
            Scope scope = CreateScope(10, 10);

            // Verify that it is empty
            Assert.AreEqual(0, scope.containedElements.Count());
        }

        [Test]
        public void ScopeContainsAddedElements()
        {
            // Create a scope
            Scope scope = CreateScope(10, 10);

            // Verify that nodes are properly added to the scope
            scope.AddElement(m_Node1);
            scope.AddElement(m_Node2);

            Assert.AreEqual(2, scope.containedElements.Count());

            Assert.IsTrue(scope.ContainsElement(m_Node1));
            Assert.AreEqual(scope, m_Node1.GetContainingScope());
            Assert.AreNotEqual(scope, m_Node1.parent);

            Assert.IsTrue(scope.ContainsElement(m_Node2));
            Assert.AreEqual(scope, m_Node2.GetContainingScope());
            Assert.AreNotEqual(scope, m_Node2.parent);
        }

        [Test]
        public void ScopeDoesNoContainRemovedElements()
        {
            // Create a scope
            Scope scope = CreateScope(10, 10);

            scope.AddElement(m_Node1);
            scope.AddElement(m_Node2);

            // Verify that a node is properly removed from its containing scope
            scope.RemoveElement(m_Node1);

            Assert.AreEqual(1, scope.containedElements.Count());
            Assert.IsFalse(scope.ContainsElement(m_Node1));
            Assert.IsNull(m_Node1.GetContainingScope());
            Assert.IsNotNull(m_Node1.parent);

            scope.RemoveElement(m_Node2);

            Assert.AreEqual(0, scope.containedElements.Count());
            Assert.IsFalse(scope.ContainsElement(m_Node2));
            Assert.IsNull(m_Node2.GetContainingScope());
            Assert.IsNotNull(m_Node2.parent);
        }

        [Test]
        public void ElementIsRemovedFromScopeWhenRemoveFromGraphView()
        {
            // Create a scope
            Scope scope = CreateScope(10, 10);

            // Verify that a node is automatically removed from its containing scope if it is removed from the graph view
            graphView.RemoveElement(m_Node2);

            Assert.AreEqual(0, scope.containedElements.Count());
            Assert.IsFalse(scope.ContainsElement(m_Node2));
            Assert.IsNull(m_Node2.GetContainingScope());
            Assert.IsNull(m_Node2.parent);
        }

        [Test]
        public void MoveToAnotherScope()
        {
            // Create two scopes
            Scope scope1 = CreateScope(10, 10);
            Scope scope2 = CreateScope(10, 10);

            // Add to scope1
            scope1.AddElement(m_Node1);

            Assert.AreEqual(1, scope1.containedElements.Count());
            Assert.IsTrue(scope1.ContainsElement(m_Node1));
            Assert.AreEqual(scope1, m_Node1.GetContainingScope());

            // ...and then move to scope2
            scope2.AddElement(m_Node1);

            Assert.AreEqual(0, scope1.containedElements.Count());
            Assert.IsFalse(scope1.ContainsElement(m_Node1));
            Assert.AreEqual(1, scope2.containedElements.Count());
            Assert.IsTrue(scope2.ContainsElement(m_Node1));
            Assert.AreEqual(scope2, m_Node1.GetContainingScope());
        }

        [Test]
        public void PreventNestedScope()
        {
            // Creates two scopes
            Scope scope1 = CreateScope(10, 10);
            Scope scope2 = CreateScope(10, 10);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => { scope1.AddElement(scope2); });

            Assert.AreEqual("Nested scope is not supported yet.", ex.Message);
        }

        [UnityTest]
        public IEnumerator ScopeWithAddedElementHasValidBounds()
        {
            // Create a scope with an empty title to determine the total (left, top, right, bottom) margins around the content of the scope
            Scope scope = CreateScope(10, 10);

            Node TestNode1 = CreateNode("Test node 1", new Vector2(100, 100), 1, 1);
            Node TestNode2 = CreateNode("Test node 2", new Vector2(200, 200), 1, 1);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            scope.AddElement(TestNode1);
            scope.AddElement(TestNode2);

            // Allow couple frames for the scope to compute its geometry (position)
            int iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            Rect contentBoundingRectInViewportSpace = GetBoundingRectInViewportSpace(scope.containedElements);
            Rect scopeGeometryInViewportSpace = scope.ChangeCoordinatesTo(graphView.contentViewContainer, scope.GetRect());

            Assert.IsTrue(scopeGeometryInViewportSpace.Contains(contentBoundingRectInViewportSpace.min));
            Assert.IsTrue(scopeGeometryInViewportSpace.Contains(contentBoundingRectInViewportSpace.max));
        }

        [UnityTest]
        public IEnumerator DisableAutoUpdateScopeGeometry()
        {
            Scope scope = CreateScope(10, 10);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Get the total margins around the content of the scope
            float leftMargin, topMargin, rightMargin, bottomMargin;

            GetScopeMargins(scope, out leftMargin, out topMargin, out rightMargin, out bottomMargin);

            Rect initialGeom = scope.GetPosition();
            Rect node1Geom = m_Node1.GetPosition();
            Rect node2Geom = m_Node2.GetPosition();

            // Disable the auto update of the geometry
            scope.autoUpdateGeometry = false;

            // Add node1
            scope.AddElement(m_Node1);

            // Add node2
            scope.AddElement(m_Node2);

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Verify that the geometry of the scope has not been recomputed to match its content
            Assert.AreEqual(initialGeom, scope.GetPosition());

            // Moves node1 and node2
            node1Geom.x -= 20;
            node1Geom.y -= 30;
            node2Geom.x += 40;
            node2Geom.y += 50;

            m_Node1.SetPosition(node1Geom);
            m_Node2.SetPosition(node2Geom);

            // Allow one frame for node1 and node2 to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Verify that the geometry of the scope has not been recomputed to match its content
            Assert.AreEqual(initialGeom, scope.GetPosition());

            // Renable the auto update of the geometry
            scope.autoUpdateGeometry = true;

            // Allow couple frames for the scope to compute its geometry (position)
            int iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Computes the bounding rect of node1 and node2
            Rect contentBoundingRectInViewportSpace = GetBoundingRectInViewportSpace(scope.containedElements);
            Rect scopeGeometryInViewportSpace = scope.ChangeCoordinatesTo(graphView.contentViewContainer, scope.GetRect());

            // Verify that the geometry of the scope has been recomputed to match its content
            Assert.AreEqual(scopeGeometryInViewportSpace, RectUtils.Inflate(contentBoundingRectInViewportSpace, leftMargin, topMargin, rightMargin, bottomMargin));
        }

        [UnityTest]
        public IEnumerator UpdateScopeGeometryAfterAddedElements()
        {
            Scope scope = CreateScope(10, 10);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Get the total margins around the content of the scope
            float leftMargin, topMargin, rightMargin, bottomMargin;

            GetScopeMargins(scope, out leftMargin, out topMargin, out rightMargin, out bottomMargin);

            Vector2 initialSize = scope.GetPosition().size;
            Rect node1Geom = m_Node1.GetPosition();
            Rect node2Geom = m_Node2.GetPosition();

            // Add node1
            scope.AddElement(m_Node1);

            // Allow couple frames for the scope to compute its geometry (position)
            int iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Computes the bounding rect of node1 and node2
            Rect contentBoundingRectInViewportSpace = GetBoundingRectInViewportSpace(scope.containedElements);
            Rect scopeGeometryInViewportSpace = scope.ChangeCoordinatesTo(graphView.contentViewContainer, scope.GetRect());

            // Verify that the geometry of node1 has not changed while the geometry of the scope has been recomputed to match its content
            Assert.AreEqual(node1Geom, m_Node1.GetPosition());
            Assert.AreEqual(scopeGeometryInViewportSpace, RectUtils.Inflate(contentBoundingRectInViewportSpace, leftMargin, topMargin, rightMargin, bottomMargin));

            // Add node2
            scope.AddElement(m_Node2);

            // Allow couple frames for the scope to compute its geometry (position)
            iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Computes the bounding rect of node1 and node2
            contentBoundingRectInViewportSpace = GetBoundingRectInViewportSpace(scope.containedElements);
            scopeGeometryInViewportSpace = scope.ChangeCoordinatesTo(graphView.contentViewContainer, scope.GetRect());

            // Verify that the geometries of node1 and node2 have not changed while the geometry of the scope has been recomputed to match its content
            Assert.AreEqual(node1Geom, m_Node1.GetPosition());
            Assert.AreEqual(node2Geom, m_Node2.GetPosition());
            Assert.AreEqual(scopeGeometryInViewportSpace, RectUtils.Inflate(contentBoundingRectInViewportSpace, leftMargin, topMargin, rightMargin, bottomMargin));
        }

        [UnityTest]
        public IEnumerator UpdateScopeGeometryAfterRemovedElements()
        {
            Scope scope = CreateScope(10, 10);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Get the total margins around the content of the scope
            float leftMargin, topMargin, rightMargin, bottomMargin;

            GetScopeMargins(scope, out leftMargin, out topMargin, out rightMargin, out bottomMargin);

            Vector2 initialSize = scope.GetPosition().size;
            Rect node2Geom = m_Node2.GetPosition();

            scope.AddElement(m_Node1);
            scope.AddElement(m_Node2);

            // Allow couple frames for the scope to compute its geometry (position)
            int iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Removes node1
            scope.RemoveElement(m_Node1);

            // Allow couple frames for the scope to compute its geometry (position)
            iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Computes the bounding rect of node2
            Rect contentBoundingRectInViewportSpace = GetBoundingRectInViewportSpace(scope.containedElements);
            Rect scopeGeometryInViewportSpace = scope.ChangeCoordinatesTo(graphView.contentViewContainer, scope.GetRect());

            // Verify that the geometry of node2 has not changed while the geometry of the scope has been recomputed
            Assert.AreEqual(node2Geom, m_Node2.GetPosition());
            Assert.AreEqual(scopeGeometryInViewportSpace, RectUtils.Inflate(contentBoundingRectInViewportSpace, leftMargin, topMargin, rightMargin, bottomMargin));

            // Removes node2
            Vector2 scope_pos = scope.GetPosition().position;

            scope.RemoveElement(m_Node2);

            // Allow couple frames for the scope to compute its geometry (position)
            iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Verifies the scope xy location has not change, only its size has been reset.
            Assert.AreEqual(new Rect(scope_pos, initialSize), scope.GetPosition());
        }

        [UnityTest]
        public IEnumerator UpdateScopeGeometryAfterMovedElements()
        {
            Scope scope = CreateScope(10, 10);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow one frame for the scope to compute its geometry (position)
            yield return null;

            // Allow one frame for the scope to update its layout
            yield return null;

            // Get the total margins around the content of the scope
            float leftMargin, topMargin, rightMargin, bottomMargin;

            GetScopeMargins(scope, out leftMargin, out topMargin, out rightMargin, out bottomMargin);

            Vector2 initialSize = scope.GetPosition().size;
            Rect node1Geom = m_Node1.GetPosition();
            Rect node2Geom = m_Node2.GetPosition();

            scope.AddElement(m_Node1);
            scope.AddElement(m_Node2);

            // Allow couple frames for the scope to compute its geometry (position)
            int iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Moves node1 and node2
            node1Geom.x -= 20;
            node1Geom.y -= 30;
            node2Geom.x += 40;
            node2Geom.y += 50;

            m_Node1.SetPosition(node1Geom);
            m_Node2.SetPosition(node2Geom);

            // Allow two frames for node1 and node2 to compute their layouts
            yield return null;
            yield return null;


            // Allow couple frames for the scope to compute its geometry (position)
            iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            // Computes the bounding rect of node1 and node2
            Rect contentBoundingRectInViewportSpace = GetBoundingRectInViewportSpace(scope.containedElements);
            Rect scopeGeometryInViewportSpace = scope.ChangeCoordinatesTo(graphView.contentViewContainer, scope.GetRect());

            // Verify that the geometries of node1 and node2 have not changed while the geometry of the scope has been recomputed
            Assert.AreEqual(node1Geom, m_Node1.GetPosition());
            Assert.AreEqual(node2Geom, m_Node2.GetPosition());
            Assert.AreEqual(scopeGeometryInViewportSpace, RectUtils.Inflate(contentBoundingRectInViewportSpace, leftMargin, topMargin, rightMargin, bottomMargin));
        }

        [UnityTest]
        public IEnumerator MoveScope()
        {
            // Create a scope and add node1, node2
            Scope scope = CreateScope(10, 10);

            scope.AddElement(m_Node1);
            scope.AddElement(m_Node2);

            // Allow one frame for the scope to be placed onto a layer and for the nodes to compute their layouts
            yield return null;

            // Allow couple frames for the scope to compute its geometry (position)
            int iteration = 0;

            while (scope.hasPendingGeometryUpdate && iteration++ < k_MaxIterations)
            {
                yield return null;
            }

            // Allow one frame for the scope to update its layout
            yield return null;

            Vector2 node1Geom = m_Node1.GetPosition().position;
            Vector2 node2Geom = m_Node2.GetPosition().position;

            // Move the scope
            Rect geometry = scope.GetPosition();

            geometry.x += 10;
            geometry.y += 10;

            scope.SetPosition(geometry);
            yield return null;

            // Verify that node1 and node2 have been moved by (10, 10)
            Assert.AreEqual(node1Geom + new Vector2(10, 10), m_Node1.GetPosition().position);
            Assert.AreEqual(node2Geom + new Vector2(10, 10), m_Node2.GetPosition().position);
        }
    }
}
