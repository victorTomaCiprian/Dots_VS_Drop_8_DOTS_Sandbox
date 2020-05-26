using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Unity.GraphElementsTests
{
    public class PlacematTests : GraphViewTester
    {
        enum TestType
        {
            Default,
            Collapsed
        }

        enum TestNodeType
        {
            Default,
            Stacks
        }

        static readonly Vector2 k_DefaultPlacematPos = new Vector2(150, 300);
        static readonly Vector2 k_DefaultPlacematSize = new Vector2(250, 250);
        static readonly Vector2 k_SecondPlacematSize = new Vector2(150, 150);
        static readonly Rect k_DefaultPlacematRect = new Rect(k_DefaultPlacematPos, k_DefaultPlacematSize);
        static readonly Vector2 k_SelectionOffset = new Vector2(35, 35);
        static readonly Vector2 k_DefaultNodeSize = new Vector2(80, 50);
        static readonly Vector2 k_DefaultStackSize = new Vector2(100, 100);

        static readonly Vector2 k_NoNode = Vector2.negativeInfinity;
        static readonly Vector2 k_NoSecondPlacemat = Vector2.negativeInfinity;

        T AddElement<T>(Rect pos) where T : GraphElement, new()
        {
            var elem = new T();
            elem.SetPosition(pos);
            elem.style.width = pos.width;
            elem.style.minWidth = pos.width / 2;
            elem.style.height = pos.height;
            elem.style.minHeight = pos.height / 2;
            graphView.AddElement(elem);
            return elem;
        }

        Node AddNode(Rect pos)
        {
            return AddElement<Node>(pos);
        }

        StackNode AddStack(Rect pos)
        {
            return AddElement<StackNode>(pos);
        }

        (Node, Port) AddNode(Rect pos, Orientation orientation, Direction direction)
        {
            var node = AddNode(pos);
            var port = node.InstantiatePort(orientation, direction, Port.Capacity.Single, typeof(float));
            if (direction == Direction.Input)
                node.inputContainer.Add(port);
            else
                node.outputContainer.Add(port);
            node.RefreshPorts();

            return (node, port);
        }

        Edge Connect(Port output, Port input)
        {
            var edge = new Edge { output = output, input = input };
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            graphView.AddElement(edge);
            return edge;
        }

        IEnumerator PlacematTestMove(Vector2 startNodePos, Vector2 startSecondPmPos, TestType testType, EventModifiers modifier)
        {
            var pmContainer = graphView.placematContainer;

            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            Node node = null;
            if (!float.IsInfinity(startNodePos.magnitude))
                node = AddNode(new Rect(startNodePos, k_DefaultNodeSize));

            Placemat pm2 = null;
            if (!float.IsInfinity(startSecondPmPos.magnitude))
            {
                pm2 = pmContainer.CreatePlacemat<Placemat>(new Rect(startSecondPmPos, k_SecondPlacematSize), pmContainer.GetTopZOrder(), "");
                pm2.Color = Color.red;
            }

            yield return null;

            bool testCollapsed = testType == TestType.Collapsed;
            if (testCollapsed)
            {
                if (node != null)
                    Assert.IsTrue(node.visible, "Node should be visible prior to main placemat collapsing.");

                if (pm2 != null)
                    Assert.IsTrue(pm2.visible, "Overlapping placemat should be visible prior to main placemat collapsing.");

                pm.Collapsed = true;
                yield return null;

                if (node != null)
                    Assert.IsFalse(node.visible, "Node should not be visible after main placemat collapsing.");

                if (pm2 != null)
                    Assert.IsFalse(pm2.visible, "Overlapping placemat should not be visible after main placemat collapsing.");
            }

            Vector2 moveDelta = new Vector2(20, 20);

            // Move!
            {
                var worldPmPosition = graphView.contentViewContainer.LocalToWorld(pm.layout.position);
                var start = worldPmPosition + k_SelectionOffset;
                var end = start + moveDelta;
                helpers.DragTo(start, end, eventModifiers: modifier);
                yield return null;
            }

            // Main placemat will always move.
            // The node and second placemat will not move if and only if Shift is pressed (so we move only the main
            // placemat) and the main placemat is not collapsed.
            Vector2 expectedPlacematPos = k_DefaultPlacematRect.position + moveDelta;
            Vector2 expectedNodePos = startNodePos;
            Vector2 expectedSecondPlacematPos = startSecondPmPos;
            string errorMessage = "have moved following manipulation.";
            if (testCollapsed || modifier != EventModifiers.Shift)
            {
                expectedNodePos += moveDelta;
                expectedSecondPlacematPos += moveDelta;
                errorMessage = "not have moved following manipulation because ";
                if (testCollapsed)
                    errorMessage += "main placemat is collapsed.";
                else
                    errorMessage += "main placemat was moved in 'slid under' mode.";
            }

            Assert.AreEqual(expectedPlacematPos, pm.GetPosition().position, "Main placemat should have moved following manipulation.");
            if (node != null)
                Assert.AreEqual(expectedNodePos, node.GetPosition().position, "Node should " + errorMessage);

            if (pm2 != null)
                Assert.AreEqual(expectedSecondPlacematPos, pm2.GetPosition().position, "Overlapping placemat should " + errorMessage);

            yield return null;

            if (testCollapsed)
            {
                pm.Collapsed = false;

                if (node != null)
                    Assert.IsTrue(node.visible, "Node should be visible after main placemat uncollapsing.");

                if (pm2 != null)
                    Assert.IsTrue(pm2.visible, "Overlapping placemat should be visible after main placemat uncollapsing.");

                yield return null;
            }
        }

        IEnumerator PlacematTestCollapseEdges(Vector2 node1Pos, Vector2 node2Pos, Orientation orientation)
        {
            var testEdges = PlacematTestCollapseEdges(node1Pos, TestNodeType.Default, node2Pos, TestNodeType.Default, orientation);
            while (testEdges.MoveNext())
                yield return null;
        }

        IEnumerator PlacematTestCollapseEdges(Vector2 node1Pos, TestNodeType node1Type, Vector2 node2Pos, TestNodeType node2Type, Orientation orientation)
        {
            var pmContainer = graphView.placematContainer;
            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            StackNode stack1 = null;
            if (node1Type == TestNodeType.Stacks)
                stack1 = AddStack(new Rect(node1Pos, k_DefaultStackSize));

            StackNode stack2 = null;
            if (node2Type == TestNodeType.Stacks)
                stack2 = AddStack(new Rect(node2Pos, k_DefaultStackSize));

            var(node1, port1) = AddNode(new Rect(node1Pos, k_DefaultNodeSize), orientation, Direction.Output);
            var(node2, port2) = AddNode(new Rect(node2Pos, k_DefaultNodeSize), orientation, Direction.Input);
            yield return null;

            stack1?.AddElement(node1);
            stack2?.AddElement(node2);
            yield return null;

            var edge = Connect(port1, port2);
            yield return null;

            bool node1Overlaps = pm.worldBound.Overlaps(node1.worldBound);
            bool node2Overlaps = pm.worldBound.Overlaps(node2.worldBound);

            Assert.IsTrue((node1Overlaps || node2Overlaps) && !(node1Overlaps && node2Overlaps),
                "One and only one node should be over the placemat");

            Port overridenPort = node1Overlaps ? port1 : port2;

            if (stack1 != null)
                Assert.IsTrue(stack1.visible, "Stack should be visible prior to placemat collapsing.");

            if (stack2 != null)
                Assert.IsTrue(stack2.visible, "Stack should be visible prior to placemat collapsing.");

            Assert.IsTrue(node1.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible prior to placemat collapsing.");

            var isPortOverridden = pm.GetPortCenterOverride(overridenPort, out var portOverridePos);
            Assert.IsFalse(isPortOverridden, "Port of visible node should not be overridden.");

            pm.Collapsed = true;
            yield return null;

            if (node1Overlaps)
            {
                if (stack1 != null)
                    Assert.IsFalse(stack1.visible, "Stack over placemat should not be visible after placemat collapse.");

                if (stack2 != null)
                    Assert.IsTrue(stack2.visible, "Stack not over placemat should still be visible after placemat collapse.");

                Assert.IsFalse(node1.visible, "Node over placemat should not be visible after placemat collapse.");
                Assert.IsTrue(node2.visible, "Node not over placemat should still be visible after placemat collapse.");
            }
            else
            {
                if (stack1 != null)
                    Assert.IsTrue(stack1.visible, "Stack not over placemat should still be visible after placemat collapse.");

                if (stack2 != null)
                    Assert.IsFalse(stack2.visible, "Stack over placemat should not be visible after placemat collapse.");

                Assert.IsTrue(node1.visible, "Node not over placemat should still be visible after placemat collapse.");
                Assert.IsFalse(node2.visible, "Node over placemat should not be visible after placemat collapse.");
            }

            Assert.IsTrue(edge.visible, "Edge crossing collapsed / uncollapsed boundary should still be visible.");

            isPortOverridden = pm.GetPortCenterOverride(overridenPort, out portOverridePos);
            Assert.IsTrue(isPortOverridden, "Port of collapsed node should be overridden.");
            if (node1Overlaps)
            {
                var edgePos = graphView.contentViewContainer.LocalToWorld(edge.edgeControl.from);
                Assert.AreEqual(portOverridePos, edgePos, "Overriden port position is not what it was expected.");
            }
            else
            {
                var edgePos = graphView.contentViewContainer.LocalToWorld(edge.edgeControl.to);
                Assert.AreEqual(portOverridePos, edgePos, "Overriden port position is not what it was expected.");
            }

            pm.Collapsed = false;
            yield return null;

            if (stack1 != null)
                Assert.IsTrue(stack1.visible, "Stack should be visible after to placemat uncollapsing.");

            if (stack2 != null)
                Assert.IsTrue(stack2.visible, "Stack should be visible after to placemat uncollapsing.");

            Assert.IsTrue(node1.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible after to placemat uncollapsing.");

            isPortOverridden = pm.GetPortCenterOverride(overridenPort, out portOverridePos);
            Assert.IsFalse(isPortOverridden, "Port of visible node should not be overridden.");
            yield return null;
        }

        IEnumerator TestStackedPlacematsMoveAndCollapse(Vector2 mouseStart, params Rect[] positions)
        {
            var pmContainer = graphView.placematContainer;

            var pms = positions.Select(p => pmContainer.CreatePlacemat<Placemat>(p, pmContainer.GetTopZOrder(), "")).ToList();
            yield return null;

            var start = mouseStart;
            var delta = Vector2.up * 50;
            var end = start + delta;
            helpers.DragTo(start, end);
            yield return null;

            // Test Move
            for (int i = 0; i < positions.Length; i++)
            {
                Assert.AreEqual(positions[i].position + delta, pms[i].layout.position, $"Placemat with zOrder {i+1} did not move properly");
            }
            yield return null;

            // Test Collapse
            for (int i = 0; i < positions.Length; i++)
            {
                Assert.True(pms[i].visible, $"Placemat with zOrder {i+1} should be visible before collapse");
            }

            pms[0].Collapsed = true;
            yield return null;

            Assert.True(pms[0].visible, "Placemat with zOrder 1 should be visible after collapse");

            for (int i = 1; i < positions.Length; i++)
            {
                Assert.False(pms[i].visible, $"Placemat with zOrder {i+1} should not be visible after collapse");
            }
            yield return null;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [Test]
        public void PlacematsZOrderSetInAdditionOrder()
        {
            var pmContainer = graphView.placematContainer;
            Assert.IsNotNull(pmContainer);

            var pm1 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            Assert.AreEqual(1, pm1.ZOrder, "Placemat has unexpected z order at creation.");

            var pm2 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            Assert.AreEqual(2, pm2.ZOrder, "Placemat has unexpected z order at creation.");

            var pm3 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            Assert.AreEqual(3, pm3.ZOrder, "Placemat has unexpected z order at creation.");

            var pm4 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            Assert.AreEqual(4, pm4.ZOrder, "Placemat has unexpected z order at creation.");
        }

        [Test]
        public void PlacematsCanBeZCycledUpAndDown()
        {
            var pmContainer = graphView.placematContainer;

            var pm1 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm2 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm3 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm4 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            var orders = new[] { 1, 2, 3, 4 };

            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders at creation.");
            //                              ^^^

            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Up);
            Assert.AreEqual(orders, new[] { pm2, pm1, pm3, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders after cycle up.");
            //                                   ^^^

            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Up);
            Assert.AreEqual(orders, new[] { pm2, pm3, pm1, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders after cycle up.");
            //                                        ^^^

            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Up);
            Assert.AreEqual(orders, new[] { pm2, pm3, pm4, pm1 }.Select(p => p.ZOrder), "Unexpected placemat z orders after cycle up.");
            //                                             ^^^

            // Once at the top, it stays at the top
            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Up);
            Assert.AreEqual(orders, new[] { pm2, pm3, pm4, pm1 }.Select(p => p.ZOrder), "Cycling up topmost placemat should be idempotent.");
            //                                             ^^^

            // Go back down
            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Down);
            Assert.AreEqual(orders, new[] { pm2, pm3, pm1, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders after cycle down.");
            //                                        ^^^

            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Down);
            Assert.AreEqual(orders, new[] { pm2, pm1, pm3, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders after cycle down.");
            //                                   ^^^

            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Down);
            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders after cycle down.");
            //                              ^^^

            // Once at the bottom, it stays at the bottom
            pmContainer.CyclePlacemat(pm1, PlacematContainer.CycleDirection.Down);
            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.ZOrder), "Cycling down bottommost placemat should be idempotent.");
            //                              ^^^
        }

        [Test]
        public void PlacematsCanBeBroughtToFrontAndBack()
        {
            var pmContainer = graphView.placematContainer;

            var pm1 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm2 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm3 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm4 = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            var orders = new[] { 1, 2, 3, 4 };

            pmContainer.BringToFront(pm1);
            Assert.AreEqual(orders, new[] { pm2, pm3, pm4, pm1 }.Select(p => p.ZOrder), "Unexpected placemat z orders after bringing to front bottommost placemat.");
            //                                             ^^^

            // BringToFront called twice is idempotent.
            pmContainer.BringToFront(pm1);
            Assert.AreEqual(orders, new[] { pm2, pm3, pm4, pm1 }.Select(p => p.ZOrder), "Bringing to front topmost placemat should be idempotent.");
            //                                             ^^^

            pmContainer.SendToBack(pm1);
            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.ZOrder), "Unexpected placemat z orders after sending to back topmost placemat.");
            //                              ^^^

            // SendToBack called twice is idempotent.
            pmContainer.SendToBack(pm1);
            Assert.AreEqual(orders, new[] { pm1, pm2, pm3, pm4 }.Select(p => p.ZOrder), "Sending to back bottommost placemat should be idempotent.");
            //                              ^^^
        }

        [UnityTest]
        public IEnumerator PlacematsCanGrowToFitNodesOnTop()
        {
            var pmContainer = graphView.placematContainer;

            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            // Nodes are *inside* the placemat
            var node1 = AddNode(new Rect(pm.layout.position - Vector2.one * 10, k_DefaultNodeSize));
            var node2 = AddNode(new Rect(pm.layout.position + pm.layout.size - Vector2.one * 10, k_DefaultNodeSize));
            yield return null;

            pm.GrowToFitElements(null);
            yield return null;

            Assert.AreEqual(node1.layout.position - new Vector2(Placemat.k_Bounds, Placemat.k_Bounds + Placemat.k_BoundTop), pm.layout.position,
                "Incorrect placemat top left position after growing it to fit nodes over it.");
            Assert.AreEqual(node2.layout.position + node2.layout.size + Vector2.one * Placemat.k_Bounds, pm.layout.position + pm.layout.size,
                "Incorrect placemat bottom right position after growing it to fit nodes over it.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacematsCanGrowToFitAnyNodes()
        {
            var pmContainer = graphView.placematContainer;

            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            // Nodes are *outside* the placemat
            var node1 = AddNode(new Rect(pm.layout.position + pm.layout.size + Vector2.one * 10, k_DefaultNodeSize));
            var node2 = AddNode(new Rect(pm.layout.position + pm.layout.size + Vector2.one * 60, k_DefaultNodeSize));
            yield return null;

            pm.GrowToFitElements(new List<GraphElement> {node1, node2});
            yield return null;

            // Since we're not snugging, the position of the placemat will remain unchanged.
            Assert.AreEqual(k_DefaultPlacematRect.position, pm.layout.position,
                "Incorrect placemat top left position after growing it to fit nodes not over it.");
            Assert.AreEqual(node2.layout.position + node2.layout.size + Vector2.one * Placemat.k_Bounds, pm.layout.position + pm.layout.size,
                "Incorrect placemat bottom right position after growing it to fit nodes not over it.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacematsCanShrinkToSnugNodesOnTop()
        {
            var pmContainer = graphView.placematContainer;

            var largeRectSize = new Rect(k_DefaultPlacematPos, k_DefaultPlacematSize * 5);
            var pm = pmContainer.CreatePlacemat<Placemat>(largeRectSize, pmContainer.GetTopZOrder(), "");

            // Nodes are *inside* the placemat
            var baseNodePos = k_DefaultPlacematPos + largeRectSize.size / 2;
            var node1 = AddNode(new Rect(baseNodePos, k_DefaultNodeSize));
            var node2 = AddNode(new Rect(baseNodePos + k_DefaultNodeSize * 2, k_DefaultNodeSize));
            yield return null;

            pm.ShrinkToFitElements(null);
            yield return null;

            Assert.AreEqual(node1.layout.position - new Vector2(Placemat.k_Bounds, Placemat.k_Bounds + Placemat.k_BoundTop), pm.layout.position,
                "Incorrect placemat top left position after shrinking it to snug nodes over it.");
            Assert.AreEqual(node2.layout.position + node2.layout.size + Vector2.one * Placemat.k_Bounds, pm.layout.position + pm.layout.size,
                "Incorrect placemat bottom right position after shrinking it to snug nodes over it.");
            yield return null;
        }

        // Config 1
        // +----------------------+
        // |       Placemat       |
        // |                      |
        // |    +------+          |
        // |    | Node |          |
        // |    |      |          |
        // |    +------+          |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveWithSingleNodeFullyOver()
        {
            var pos = k_DefaultPlacematPos + Vector2.one * 50;
            var actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        [UnityTest]
        public IEnumerator CollapsedPlacematMoveWithSingleNodeFullyOver()
        {
            var pos = k_DefaultPlacematPos + Vector2.one * 50;
            var actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Collapsed, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Collapsed, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 2
        // +----------------------+
        // |       Placemat       |
        // |                      |
        // |                      |
        // |                      |
        // |                      |
        // |                   +------+
        // |                   | Node |
        // +-------------------|      |
        //                     +------+
        [UnityTest]
        public IEnumerator PlacematMoveWithSingleNodePartiallyOver()
        {
            var pos = k_DefaultPlacematPos + k_DefaultPlacematSize - Vector2.one * 5;
            var actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        [UnityTest]
        public IEnumerator CollapsedPlacematMoveWithSingleNodePartiallyOver()
        {
            var pos = k_DefaultPlacematPos + k_DefaultPlacematSize - Vector2.one * 5;
            var actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Collapsed, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(pos, k_NoSecondPlacemat, TestType.Collapsed, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 3
        // +----------------------+
        // |       Placemat       |
        // |                      |
        // |                      |  +------+
        // |                      |  | Node |
        // |                      |  |      |
        // |                      |  +------+
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveUnderExternalNodeWithoutEffect()
        {
            var pmContainer = graphView.placematContainer;

            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            Vector2 startNodePos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x + k_DefaultNodeSize.x, k_DefaultNodeSize.y / 2);
            var node = new Node();
            node.SetPosition(new Rect(startNodePos, k_DefaultNodeSize));
            node.style.width = k_DefaultNodeSize.x;
            node.style.height = k_DefaultNodeSize.y;
            graphView.AddElement(node);
            yield return null;

            const int steps = 10;
            Vector2 moveDelta = new Vector2(2 * k_DefaultPlacematSize.x / steps, 0);

            // Move!
            {
                var worldPmPosition = graphView.contentViewContainer.LocalToWorld(k_DefaultPlacematRect.position);
                var start = worldPmPosition + k_SelectionOffset;
                var end = start + moveDelta;
                helpers.MouseDownEvent(start);
                yield return null;

                for (int i = 0; i < steps; i++)
                {
                    // Make sure we get under the node
                    helpers.MouseDragEvent(start, end);
                    yield return null;

                    start = end;
                    end += moveDelta;
                }

                helpers.MouseUpEvent(end);
                yield return null;
            }

            // The placemat will have moved, but not the node.
            Vector2 expectedPlacematPos = k_DefaultPlacematRect.position + moveDelta * steps;
            Vector2 expectedNodePos = startNodePos;
            Assert.AreEqual(expectedPlacematPos, pm.GetPosition().position, "Placemat should have moved following manipulation.");
            Assert.AreEqual(expectedNodePos, node.GetPosition().position, "Node should not have moved when placemat was moved under it.");
            yield return null;
        }

        // Config 4
        // +----------------------+
        // |       Placemat1      |
        // |                      |
        // |   +-------------+    |
        // |   |  Placemat2  |    |
        // |   |             |    |
        // |   +-------------+    |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSinglePlacematFullyOver()
        {
            var pos = k_DefaultPlacematPos + Vector2.one * 50;
            var actions = PlacematTestMove(k_NoNode, pos, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(k_NoNode, pos, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 5
        // +----------------------+
        // |       Placemat1      |
        // |                      |
        // |   +-------------+    |
        // |   |  Placemat2  |    |
        // |   |             |    |
        // |   |  +------+   |    |
        // |   |  | Node |   |    |
        // |   |  +------+   |    |
        // |   |             |    |
        // |   +-------------+    |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodeFullyOverPlacematFullyOver()
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var nodePos = pm2Pos + Vector2.one * 50;
            var actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 6
        // +----------------------+
        // |       Placemat1      |
        // |                      |
        // |   +-------------+    |
        // |   |  Placemat2  |    |
        // |   |             |    |
        // |   |        +------+  |
        // |   |        | Node |  |
        // |   |        +------+  |
        // |   |             |    |
        // |   +-------------+    |
        // |                      |
        // +----------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodePartiallyOverPlacematFullyOver()
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var nodePos = pm2Pos + new Vector2(k_SecondPlacematSize.x - k_DefaultNodeSize.x / 2, 50);
            var actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 7
        // +------------------+
        // |    Placemat1     |
        // |                  |
        // |                +-------------+
        // |                |  Placemat2  |
        // |                |             |
        // |                |    +------+ |
        // |                |    | Node | |
        // |                |    +------+ |
        // |                |             |
        // |                +-------------+
        // |                  |
        // +------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodeFullyOverPlacematPartiallyOver()
        {
            var pm2Pos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x - 25, 50);
            var nodePos = pm2Pos + Vector2.one * 50;
            var actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 8
        // +------------------+
        // |    Placemat1     |
        // |                  |
        // |                +-------------+
        // |                |  Placemat2  |
        // |                |             |
        // |                |    +------+ |
        // |                +----| Node |-+
        // |                  |  +------+
        // |                  |
        // +------------------+
        [UnityTest]
        public IEnumerator PlacematMoveSingleNodePartiallyOverPlacematPartiallyOver()
        {
            var pm2Pos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x - 25, 50);
            var nodePos = pm2Pos + new Vector2(50, k_SecondPlacematSize.y - 25);
            var actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.None);
            while (actions.MoveNext())
                yield return null;

            actions = PlacematTestMove(nodePos, pm2Pos, TestType.Default, EventModifiers.Shift);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 9
        // +------------------+
        // |    Placemat1     |
        // |                  |
        // |                  |-----------+
        // |                  | Placemat2 |
        // |                  |           |
        // |                  |  +------+ |
        // |                  |  | Node | |
        // |                  |  +------+ |
        // |                  |           |
        // |                  |-----------+
        // |                  |
        // +------------------+
        [UnityTest]
        public IEnumerator PlacematDoesNotMoveSingleNodeFullyOverPlacematUnder()
        {
            var pm2Pos = k_DefaultPlacematPos + new Vector2(k_DefaultPlacematSize.x - 25, 50);
            var nodePos = pm2Pos + Vector2.one * 50;

            var pmContainer = graphView.placematContainer;
            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm2 = pmContainer.CreatePlacemat<Placemat>(new Rect(pm2Pos, k_SecondPlacematSize), pmContainer.GetTopZOrder(), "");
            pm2.Color = Color.red;
            var node = AddNode(new Rect(nodePos, k_DefaultNodeSize));

            pmContainer.SendToBack(pm2);

            Vector2 moveDelta = new Vector2(20, 20);

            // Move!
            {
                var worldPmPosition = graphView.contentViewContainer.LocalToWorld(pm.layout.position);
                var start = worldPmPosition + k_SelectionOffset;
                var end = start + moveDelta;
                helpers.DragTo(start, end);
                yield return null;
            }

            Vector2 expectedPlacematPos = k_DefaultPlacematRect.position + moveDelta;

            // Node and second placemat should not have moved since they are below main placemat
            Vector2 expectedNodePos = nodePos;
            Vector2 expectedSecondPlacematPos = pm2Pos;

            Assert.AreEqual(expectedPlacematPos, pm.GetPosition().position, "Placemat should have moved following manipulation.");
            Assert.AreEqual(expectedNodePos, node.GetPosition().position, "Node should not have moved.");
            Assert.AreEqual(expectedSecondPlacematPos, pm2.GetPosition().position, "Placemat should have moved because it was under the placemat being manipulated.");
            yield return null;

            pm.Collapsed = true;

            // Items under a collapsed placemat will not be hidden.
            Assert.IsTrue(node.visible, "Node should be visible since not over collapsed placemat.");
            Assert.IsTrue(pm2.visible, "Placemat should be visible since it was under collapsed placemat.");

            yield return null;
        }

        // Config 10 (edges)
        // +-------------+
        // | v Placemat  |
        // |             |
        // |  +-------+  |  +-------+       +-------------+   +-------+
        // |  | Node1 o-----o Node2 |  >>>  | > Placemat -----o Node2 |
        // |  +-------+  |  +-------+       +-------------+   +-------+
        // |             |
        // +-------------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheEastOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematPos + k_DefaultPlacematSize / 2 + Vector2.right * k_DefaultPlacematSize.x;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, Orientation.Horizontal);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 11 (edges)
        //             +-------------+
        //             | v Placemat  |
        //             |             |
        //  +-------+  |   +-------+ |       +-------+    +-------------+
        //  | Node1 o----- o Node2 | |  >>>  | Node1 o------ > Placemat |
        //  +-------+  |   +-------+ |       +-------+    +-------------+
        //             |             |
        //             +-------------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheWestOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos - Vector2.right * 150;
            var node2Pos = k_DefaultPlacematPos + Vector2.one * 50;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, Orientation.Horizontal);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 12 (edges)
        // +-------------+
        // | v Placemat  |
        // |             |
        // |  +-------+  |        +-------------+
        // |  | Node1 |  |  >>>   | > Placemat  |
        // |  +---o---+  |        +------|------+
        // |      |      |               |
        // +------|------+               |
        //        |                      |
        //    +---o---+              +---o---+
        //    | Node2 |              | Node2 |
        //    +-------+              +-------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheSouthOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematPos + k_DefaultPlacematSize / 2 + Vector2.up * 200;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, Orientation.Vertical);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 13 (edges)
        //    +-------+              +-------+
        //    | Node1 |              | Node1 |
        //    +---o---+              +---o---+
        //        |                      |
        // +------|------+               |
        // | v Pla|emat  |               |
        // |      |      |               |
        // |  +---o---+  |        +------|------+
        // |  | Node2 |  |  >>>   | > Placemat  |
        // |  +-------+  |        +-------------+
        // |             |
        // +-------------+
        [UnityTest]
        public IEnumerator SingleConnectedToTheNorthOverCollapsedPlacematHidesNodeAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + k_DefaultPlacematSize / 2 - Vector2.up * 200;
            var node2Pos = k_DefaultPlacematPos + Vector2.one * 50;

            var actions = PlacematTestCollapseEdges(node1Pos, node2Pos, Orientation.Vertical);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 14 (edges)
        // +------------------------+
        // |        Placemat        |
        // |                        |       +-------------+
        // | +-------+    +-------+ |  >>>  | > Placemat  |
        // | | Node1 o----o Node2 | |       +-------------+
        // | +-------+    +-------+ |
        // |                        |
        // +------------------------+
        [UnityTest]
        public IEnumerator TwoConnectedNodesOverCollapsedPlacematHideBothNodesAndEdge()
        {
            var pmContainer = graphView.placematContainer;
            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var node1Pos = pm.layout.position + Vector2.one * 50;
            var node2Pos = pm.layout.position + pm.layout.size - Vector2.one * 100;

            var(node1, port1) = AddNode(new Rect(node1Pos, k_DefaultNodeSize), Orientation.Horizontal, Direction.Output);
            var(node2, port2) = AddNode(new Rect(node2Pos, k_DefaultNodeSize), Orientation.Horizontal, Direction.Input);
            var edge = Connect(port1, port2);

            yield return null;
            Assert.IsTrue(node1.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible prior to placemat collapsing.");

            pm.Collapsed = true;
            yield return null;

            Assert.IsFalse(node1.visible, "Node over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(node2.visible, "Node over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(edge.visible, "Edge connecting two nodes over placemat should not be visible after placemat collapse.");

            pm.Collapsed = false;
            yield return null;

            Assert.IsTrue(node1.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible after to placemat uncollapsing.");

            yield return null;
        }

        // Config 15 (edges)
        // +-----------------+
        // |     Placemat    |
        // |                 |
        // |  +-----------+  |
        // |  |   Stack   |  |
        // |  |           |  |
        // |  | +-------+ |  |   +-------+       +-------------+   +-------+
        // |  | | Node1 o--------o Node2 |  >>>  | > Placemat -----o Node2 |
        // |  | +-------+ |  |   +-------+       +-------------+   +-------+
        // |  |           |  |
        // |  +-----------+  |
        // |                 |
        // +-----------------+
        [UnityTest]
        public IEnumerator SingleConnectedStackToTheEastOverCollapsedPlacematHidesStackAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematPos + k_DefaultPlacematSize + Vector2.right * 100;

            var actions = PlacematTestCollapseEdges(node1Pos, TestNodeType.Stacks, node2Pos, TestNodeType.Default, Orientation.Horizontal);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 16 (edges)
        //             +-----------------+
        //             |    Placemat     |
        //             |                 |
        //             |  +-----------+  |
        //             |  |   Stack   |  |
        //             |  |           |  |
        // +-------+   |  | +-------+ |  |       +------+   +-------------+
        // | Node1 o--------o Node2 | |  |  >>>  | Node o----- > Placemat |
        // +-------+   |  | +-------+ |  |       +------+   +-------------+
        //             |  |           |  |
        //             |  +-----------+  |
        //             |                 |
        //             +-----------------+
        [UnityTest]
        public IEnumerator SingleConnectedStackToTheWestOverCollapsedPlacematHidesStackAndRedirectsEdge()
        {
            var node1Pos = k_DefaultPlacematPos - Vector2.right * 100;
            var node2Pos = k_DefaultPlacematPos + Vector2.one * 50;

            var actions = PlacematTestCollapseEdges(node1Pos, TestNodeType.Default, node2Pos, TestNodeType.Stacks, Orientation.Horizontal);
            while (actions.MoveNext())
                yield return null;
        }

        // Config 17 (edges)
        // +-----------------------------+
        // |           Placemat          |
        // |                             |
        // |  +-----------+              |
        // |  |   Stack   |              |
        // |  |           |              |       +-------------+
        // |  | +-------+ |   +-------+  |  >>>  | > Placemat  |
        // |  | | Node1 o-----o Node2 |  |       +-------------+
        // |  | +-------+ |   +-------+  |
        // |  |           |              |
        // |  +-----------+              |
        // |                             |
        // +-----------------------------+
        [UnityTest]
        public IEnumerator SingleConnectedStackToTheEastBothOverCollapsedPlacematHidesStackNodeAndEdge()
        {
            var pmContainer = graphView.placematContainer;
            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");

            var node1Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node2Pos = k_DefaultPlacematPos + k_DefaultPlacematSize - k_DefaultNodeSize * 1.5f;

            var stack = AddStack(new Rect(node1Pos, k_DefaultStackSize));

            var(node1, port1) = AddNode(new Rect(node1Pos, k_DefaultNodeSize), Orientation.Horizontal, Direction.Output);
            var(node2, port2) = AddNode(new Rect(node2Pos, k_DefaultNodeSize), Orientation.Horizontal, Direction.Input);
            yield return null;

            stack.AddElement(node1);
            yield return null;

            var edge = Connect(port1, port2);
            yield return null;

            bool node1Overlaps = pm.worldBound.Overlaps(node1.worldBound);
            bool node2Overlaps = pm.worldBound.Overlaps(node2.worldBound);

            Assert.IsTrue(node1Overlaps && node2Overlaps, "Both node should be over the placemat");

            Assert.IsTrue(stack.visible, "Stack should be visible prior to placemat collapsing.");
            Assert.IsTrue(node1.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible prior to placemat collapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible prior to placemat collapsing.");

            pm.Collapsed = true;
            yield return null;

            Assert.IsFalse(stack.visible, "Stack over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(node1.visible, "Node over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(node2.visible, "Node over placemat should not be visible after placemat collapse.");
            Assert.IsFalse(edge.visible, "Edge over placemat should not be visible after placemat collapse.");

            pm.Collapsed = false;
            yield return null;

            Assert.IsTrue(stack.visible, "Stack should be visible after to placemat uncollapsing.");
            Assert.IsTrue(node1.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(node2.visible, "Node should be visible after to placemat uncollapsing.");
            Assert.IsTrue(edge.visible, "Edge should be visible after to placemat uncollapsing.");
            yield return null;
        }

        // If Placemat1 is collapsed, Node is hidden. Moving Placemat2 should not move the node.
        //
        //        +---------------+   +---------------+
        //        |   Placemat1   |   |   Placemat2   |
        //        |            +----------+           |
        //        |            |   Node   |           |
        //        |            |          |           |
        //        |            +----------+           |
        //        |               |   |               |
        //        +---------------+   +---------------+
        [UnityTest]
        public IEnumerator PlacematDoesNotMoveElementHiddenByOtherPlacemat()
        {
            var pmContainer = graphView.placematContainer;

            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            pm.Color = Color.red;

            float xOffset = k_DefaultPlacematSize.x - k_DefaultNodeSize.x / 2;
            Vector2 nodePos = k_DefaultPlacematPos + Vector2.right * xOffset;
            Node node = AddNode(new Rect(nodePos, k_DefaultNodeSize));

            Vector2 pm2Pos = k_DefaultPlacematPos + Vector2.right * (k_DefaultPlacematSize.x + 10f);
            var pm2 = pmContainer.CreatePlacemat<Placemat>(new Rect(pm2Pos, k_DefaultPlacematSize), pmContainer.GetTopZOrder(), "");
            pm2.Color = Color.green;
            yield return null;

            pm.Collapsed = true;
            yield return null;

            Assert.AreEqual(nodePos, node.layout.position);

            // Move pm2
            {
                var worldPm2Position = graphView.contentViewContainer.LocalToWorld(pm2.layout.position);
                var start = worldPm2Position + k_SelectionOffset;
                var end = start + (Vector2.right * 100);
                helpers.DragTo(start, end);
                yield return null;
            }

            Assert.AreEqual(nodePos, node.layout.position);
            yield return null;
        }

        //               +------------------+
        //               |    Placemat 1    |
        //               |                  |
        //   +------------------+   +------------------+
        //   |    Placemat 2    |   |    Placemat 3    |
        //   |                  |---|                  |
        //   |           +------------------+          |
        //   |           |    Placemat 4    |          |
        //   +-----------|                  |----------+
        //               |                  |
        //               +------------------+
        [UnityTest]
        public IEnumerator PlacematDiamondMovesProperly()
        {
            var pos = new[]
            {
                new Rect(300, 100, 200, 100),
                new Rect(175, 175, 200, 100),
                new Rect(425, 175, 200, 100),
                new Rect(300, 250, 200, 100)
            };
            var actions = TestStackedPlacematsMoveAndCollapse(pos[0].center, pos);

            while (actions.MoveNext())
                yield return null;
        }

        //                                   +------------------+
        //                                   |    Placemat 1    |
        //                                   |                  |
        //                       +------------------+   +------------------+
        //                       |    Placemat 2    |   |    Placemat 3    |
        //                       |                  |---|                  |
        //             +------------------+  +------------------+  +------------------+
        //             |    Placemat 4    |  |    Placemat 5    |  |    Placemat 6    |
        //             |                  |--|                  |--|                  |
        //   +------------------+  +------------------+  +------------------+  +------------------+
        //   |    Placemat 7    |  |    Placemat 8    |  |    Placemat 9    |  |    Placemat 10   |
        //   |                  |--|                  |  |                  |--|                  |
        //   |                  |  |                  |  |                  |  |                  |
        //   +------------------+  +------------------+  +------------------+  +------------------+
        [UnityTest]
        public IEnumerator PlacematPyramidMovesProperly()
        {
            var pos = new[]
            {
                new Rect(425, 50, 200, 100),

                new Rect(300, 125, 200, 100),
                new Rect(550, 125, 200, 100),

                new Rect(175, 200, 200, 100),
                new Rect(425, 200, 200, 100),
                new Rect(675, 200, 200, 100),

                new Rect(50, 275, 200, 100),
                new Rect(300, 275, 200, 100),
                new Rect(550, 275, 200, 100),
                new Rect(800, 275, 200, 100)
            };
            var actions = TestStackedPlacematsMoveAndCollapse(pos[0].center, pos);

            while (actions.MoveNext())
                yield return null;
        }

        //   +--------------------------------------------------------------------+
        //   |                             Placemat 1                             |
        //   |                                                                    |
        //   |  +------------------+  +------------------+  +------------------+  |
        //   +--|    Placemat 2    |--|    Placemat 3    |--|    Placemat 4    |--+
        //      |                  |  |                  |  |                  |
        //   +--------------------------------------------------------------------+
        //   |                             Placemat 5                             |
        //   |                                                                    |
        //   +--------------------------------------------------------------------+
        [UnityTest]
        public IEnumerator PlacematSandwichMovesProperly()
        {
            var pos = new[]
            {
                new Rect(50,  50, 825, 100),

                new Rect(75, 125, 200, 100),
                new Rect(325, 125, 200, 100),
                new Rect(575, 125, 200, 100),

                new Rect(50, 200, 825, 100)
            };
            var actions = TestStackedPlacematsMoveAndCollapse(pos[0].center, pos);

            while (actions.MoveNext())
                yield return null;
            yield return null;
        }

        // +-----------------------------+       +-----------------------------+
        // |           Placemat          |       |           Placemat          |
        // |                             |       |                             |
        // |  +-------------+            |       |  +-------------+            |
        // |  | v Placemat  |            |       |  | > Placemat  |            |
        // |  |             |            |  ==>  |  +-------------+            |  ==>  +-----------------------------+  ==>  +-----------------------------+
        // |  |   +------+  |            |       |                             |       |           Placemat          |       |           Placemat          |
        // |  |   | Node |  |            |       |                             |       |                             |       |                             |
        // |  |   +------+  |            |       |                             |       |  +-------------+            |       |  +-------------+            |
        // |  +-------------+            |       |                             |       |  | > Placemat  |            |       |  | v Placemat  |            |
        // +-----------------------------+       +-----------------------------+       |  +-------------+            |       |  |             |            |
        //                                                                             |                             |       |  |   +------+  |            |
        //                                                                             |                             |       |  |   | Node |  |            |
        //                                                                             |                             |       |  |   +------+  |            |
        //                                                                             |                             |       |  +-------------+            |
        //                                                                             +-----------------------------+       +-----------------------------+
        [UnityTest]
        public IEnumerator CollapsedPlacematMovedByPlacematMovesNodeCorrectly()
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var nodePos = pm2Pos + Vector2.one * 50;

            var pmContainer = graphView.placematContainer;

            pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm2 = pmContainer.CreatePlacemat<Placemat>(new Rect(pm2Pos, k_SecondPlacematSize), pmContainer.GetTopZOrder(), "");
            pm2.Color = Color.red;

            var node = AddNode(new Rect(nodePos, k_DefaultNodeSize));
            yield return null;

            Assert.True(node.visible, "Node should be visible prior to collapse");
            pm2.Collapsed = true;
            yield return null;

            Assert.False(node.visible, "Node should not be visible after collapse");
            yield return null;

            var worldPmPosition = graphView.contentViewContainer.LocalToWorld(k_DefaultPlacematRect.position);
            var start = worldPmPosition + k_SelectionOffset;
            var delta = Vector2.one * 10;
            var end = start + delta;
            helpers.DragTo(start, end);
            yield return null;

            pm2.Collapsed = false;
            yield return null;

            Assert.True(node.visible, "Node should be visible after uncollapse");
            Assert.AreEqual(nodePos + delta, node.GetPosition().position, "Node should have moved with the placemat hiding it");
            yield return null;
        }

        // +-------------------+
        // |      Placemat     |
        // |                   |
        // |  +-------------+  |    +-------+                                +-------+
        // |  | > Placemat ---------o  Node |  ==>  +-------------------+  --o  Node |
        // |  +-------------+  |    +-------+       |      Placemat     | /  +-------+
        // |                   |                    |                   |/
        // +-------------------+                    |  +-------------+  /
        //                                          |  | > Placemat ---/|
        //                                          |  +-------------+  |
        //                                          |                   |
        //                                          +-------------------+
        [UnityTest]
        public IEnumerator CollapsedPlacematWithEdgesMovedByPlacematMovesEdgesCorrectly()
        {
            var pm2Pos = k_DefaultPlacematPos + Vector2.one * 50;
            var node1Pos = pm2Pos + Vector2.one * 50;
            var node2Pos = node1Pos + Vector2.right * 200;

            var pmContainer = graphView.placematContainer;

            pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var pm2 = pmContainer.CreatePlacemat<Placemat>(new Rect(pm2Pos, k_SecondPlacematSize), pmContainer.GetTopZOrder(), "");
            pm2.Color = Color.red;

            var(node1, port1) = AddNode(new Rect(node1Pos, k_DefaultNodeSize), Orientation.Horizontal, Direction.Output);
            var(node2, port2) = AddNode(new Rect(node2Pos, k_DefaultNodeSize), Orientation.Horizontal, Direction.Input);
            yield return null;

            var edge = Connect(port1, port2);
            yield return null;

            Assert.True(node1.visible, "Node should be visible prior to collapse");
            Assert.True(node2.visible, "Node should be visible prior to collapse");
            Assert.True(edge.visible, "Edge should be visible prior to collapse");
            pm2.Collapsed = true;
            yield return null;

            Assert.False(node1.visible, "Node should not be visible after collapse");
            Assert.True(node2.visible, "Node should still be visible after collapse");
            Assert.True(edge.visible, "Edge should still be visible after collapse");

            var isPortOverridden = pm2.GetPortCenterOverride(port1, out var portOverridePos);
            Assert.IsTrue(isPortOverridden, "Port of hidden node should be overridden.");
            var edgeFromPos = graphView.contentViewContainer.LocalToWorld(edge.edgeControl.from);
            Assert.AreEqual(portOverridePos, edgeFromPos, "Overriden port position is not what it was expected.");
            yield return null;

            var worldPmPosition = graphView.contentViewContainer.LocalToWorld(k_DefaultPlacematRect.position);
            var start = worldPmPosition + k_SelectionOffset;
            var delta = Vector2.one * 10;
            var end = start + delta;
            helpers.DragTo(start, end);
            yield return null;

            isPortOverridden = pm2.GetPortCenterOverride(port1, out portOverridePos);
            Assert.IsTrue(isPortOverridden, "Port of hidden node should still be overridden.");
            edgeFromPos = graphView.contentViewContainer.LocalToWorld(edge.edgeControl.from);
            Assert.AreEqual(portOverridePos, edgeFromPos, "Overriden port position is not what it was expected after move.");
            yield return null;
        }

        [Test]
        public void PlacematSetPositionSetsUncollapsedSizeWhenUncollapsed()
        {
            var pmContainer = graphView.placematContainer;
            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            var newRect = new Rect(-42, 4242, 24, 242);
            pm.SetPosition(newRect);
            Assert.AreEqual(newRect.size, pm.UncollapsedSize);

            pm.Collapsed = true;
            pm.SetPosition(k_DefaultPlacematRect);
            Assert.AreEqual(newRect.size, pm.UncollapsedSize);
        }

        [UnityTest]
        public IEnumerator SettingCollapsedElementsWorks()
        {
            var pmContainer = graphView.placematContainer;
            var pm = pmContainer.CreatePlacemat<Placemat>(k_DefaultPlacematRect, pmContainer.GetTopZOrder(), "");
            pm.Collapsed = true;

            // ReSharper disable once Unity.InefficientMultiplicationOrder
            Vector2 nodePos = k_DefaultPlacematPos + Vector2.down * 2 * Placemat.k_DefaultCollapsedSize;
            Node node = AddNode(new Rect(nodePos, k_DefaultNodeSize));
            yield return null;

            Assert.IsFalse(node.style.visibility == Visibility.Hidden);

            pm.SetCollapsedElements(new[] {node});
            yield return null;

            Assert.IsTrue(node.style.visibility == Visibility.Hidden);
            yield return null;
        }
    }
}
