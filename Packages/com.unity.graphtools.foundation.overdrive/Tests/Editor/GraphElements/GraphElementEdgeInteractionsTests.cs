using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphElementEdgeInteractionsTests : GraphViewTester
    {
        Node firstNode { get; set; }
        Node secondNode { get; set; }
        Port startPort { get; set; }
        Port endPort { get; set; }
        Port startPortTwo { get; set; }
        Port endPortTwo { get; set; }

        static readonly Vector2 k_FirstNodePosition = new Vector2(0, 0);
        static readonly Vector2 k_SecondNodePosition = new Vector2(400, 0);
        const float k_EdgeSelectionOffset = 20.0f;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            firstNode = CreateNode("First Node", k_FirstNodePosition, 0, 2);
            secondNode = CreateNode("Second Node", k_SecondNodePosition, 2, 0);

            startPort = firstNode.outputContainer[0] as Port;
            endPort = secondNode.inputContainer[0] as Port;
            startPortTwo = firstNode.outputContainer[1] as Port;
            endPortTwo = secondNode.inputContainer[1] as Port;
        }

        [UnityTest]
        public IEnumerator MixedOrientationEdges()
        {
            Node horizontalNode = CreateNode("Horizontal Node", new Vector2(100, 200), 1, 1, Orientation.Horizontal);
            Node verticalNode = CreateNode("Vertical Node", new Vector2(500, 100), 1, 1, Orientation.Vertical);

            yield return null;

            Port outputPort = horizontalNode.outputContainer[0] as Port;
            Port inputPort = verticalNode.inputContainer[0] as Port;
            helpers.DragTo(outputPort.GetGlobalCenter(), inputPort.GetGlobalCenter());

            Edge edge = outputPort.connections.ToList()[0];
            Assert.AreEqual(inputPort, edge.input);
            Assert.AreEqual(outputPort, edge.output);
            Assert.AreEqual(Orientation.Vertical, edge.edgeControl.inputOrientation);
            Assert.AreEqual(Orientation.Horizontal, edge.edgeControl.outputOrientation);

            outputPort = verticalNode.outputContainer[0] as Port;
            inputPort = horizontalNode.inputContainer[0] as Port;
            helpers.DragTo(outputPort.GetGlobalCenter(), inputPort.GetGlobalCenter());

            edge = outputPort.connections.ToList()[0];
            Assert.AreEqual(inputPort, edge.input);
            Assert.AreEqual(outputPort, edge.output);
            Assert.AreEqual(Orientation.Horizontal, edge.edgeControl.inputOrientation);
            Assert.AreEqual(Orientation.Vertical, edge.edgeControl.outputOrientation);
        }

        [UnityTest]
        public IEnumerator EdgeConnectOnSinglePortOutputToInputWorks()
        {
            // We start without any connection
            Assert.AreEqual(0, startPort.connections.Count());
            Assert.AreEqual(0, endPort.connections.Count());

            // Drag an edge between the two ports
            helpers.DragTo(startPort.GetGlobalCenter(), endPort.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(1, startPort.connections.Count());
            Assert.AreEqual(1, endPort.connections.Count());
            Assert.IsTrue(startPort.connections.First() == endPort.connections.First());

            Edge edge = startPort.connections.First();
            Assert.IsNotNull(edge);
            Assert.IsNotNull(edge.parent);
        }

        // TODO Add Test multi port works
        // TODO Add Test multi connection to single port replaces connection
        // TODO Add Test disallow multiple edges on same multiport pairs (e.g. multiple edges between output A and input B)

        [UnityTest]
        public IEnumerator EdgeConnectOnSinglePortInputToOutputWorks()
        {
            // We start without any connection
            Assert.AreEqual(0, startPort.connections.Count());
            Assert.AreEqual(0, endPort.connections.Count());

            // Drag an edge between the two ports
            helpers.DragTo(endPort.GetGlobalCenter(), startPort.GetGlobalCenter());

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            // Check that the edge exists and that it connects the two ports.
            Assert.AreEqual(1, startPort.connections.Count());
            Assert.AreEqual(1, endPort.connections.Count());
            Assert.IsTrue(startPort.connections.First() == endPort.connections.First());

            Edge edge = startPort.connections.First();
            Assert.IsNotNull(edge);
            Assert.IsNotNull(edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectInputWorks()
        {
            float startPortX = startPort.GetGlobalCenter().x;
            float endPortX = endPort.GetGlobalCenter().x;
            float endPortY = endPort.GetGlobalCenter().y;

            // Create the edge to be tested.
            Edge edge = CreateEdge(startPort, endPort);
            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, endPortY);
            helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);

            Assert.AreEqual(startPort, edge.output);
            Assert.IsNull(edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);

            Assert.IsNull(edge.output);
            Assert.IsNull(edge.input);
            Assert.IsNull(edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectOutputWorks()
        {
            float startPortX = startPort.GetGlobalCenter().x;
            float startPortY = startPort.GetGlobalCenter().y;
            float endPortX = endPort.GetGlobalCenter().x;

            // Create the edge to be tested.
            Edge edge = CreateEdge(startPort, endPort);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);

            Assert.IsNull(edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);

            Assert.IsNull(edge.output);
            Assert.IsNull(edge.input);
            Assert.IsNull(edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeReconnectInputWorks()
        {
            float endPortX = endPort.GetGlobalCenter().x;
            float endPortY = endPort.GetGlobalCenter().y;

            // Create the edge to be tested.
            Edge edge = CreateEdge(startPort, endPort);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);

            // Mouse move to the second port while holding CTRL.
            var portTwoAreaPos = endPortTwo.GetGlobalCenter();
            helpers.MouseDragEvent(edgeRightSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);

            Assert.AreEqual(startPort, edge.output);
            Assert.IsNull(edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(portTwoAreaPos);

            Assert.AreEqual(startPort, edge.output);
            Assert.AreEqual(endPortTwo, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeReconnectOutputWorks()
        {
            float startPortX = startPort.GetGlobalCenter().x;
            float startPortY = startPort.GetGlobalCenter().y;

            // Create the edge to be tested.
            Edge edge = CreateEdge(startPort, endPort);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);

            // Mouse move to the second port while holding CTRL.
            var portTwoAreaPos = startPortTwo.GetGlobalCenter();
            helpers.MouseDragEvent(edgeLeftSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);

            Assert.IsNull(edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(portTwoAreaPos);

            Assert.AreEqual(startPortTwo, edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnInput()
        {
            float startPortX = startPort.GetGlobalCenter().x;
            float endPortX = endPort.GetGlobalCenter().x;
            float endPortY = endPort.GetGlobalCenter().y;

            // Create the edge to be tested.
            Edge edge = CreateEdge(startPort, endPort);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, endPortY);
            helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);

            Assert.AreEqual(startPort, edge.output);
            Assert.IsNull(edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            helpers.KeyDownEvent(KeyCode.Escape);

            Assert.AreEqual(startPort, edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            helpers.KeyUpEvent(KeyCode.Escape);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);

            Assert.AreEqual(startPort, edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnOutput()
        {
            float startPortX = startPort.GetGlobalCenter().x;
            float startPortY = startPort.GetGlobalCenter().y;
            float endPortX = endPort.GetGlobalCenter().x;

            // Create the edge to be tested.
            Edge edge = CreateEdge(startPort, endPort);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);

            Assert.IsNull(edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            helpers.KeyDownEvent(KeyCode.Escape);

            Assert.AreEqual(startPort, edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            helpers.KeyUpEvent(KeyCode.Escape);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);

            Assert.AreEqual(startPort, edge.output);
            Assert.AreEqual(endPort, edge.input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeConnectionUnderThresholdDistanceNotEffective()
        {
            Port startPort = firstNode.outputContainer[1] as Port;
            var startPos = startPort.GetGlobalCenter();
            helpers.DragTo(startPos, startPos + new Vector3(EdgeConnector<Edge>.k_ConnectionDistanceTreshold / 2f, 0f, 0f));

            yield return null;

            Port inputPort = secondNode.inputContainer[0] as Port;
            Assert.AreEqual(0, inputPort.connections.Count());

            yield return null;
        }
    }
}
