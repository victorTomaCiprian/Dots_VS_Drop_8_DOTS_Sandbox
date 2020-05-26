using System.Collections;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.GraphElementsTests
{
    public class GraphElementFrameTests : GraphViewTester
    {
        class FooNode : Node
        {
        }

        class BarNode : Node
        {
        }

        [UnityTest]
        public IEnumerator FrameSelectedNodeAndEdge()
        {
            Vector2 firstNodePosition = new Vector2(1000, 1000);
            Vector2 secondNodePosition = new Vector2(1200, 1200);

            var firstNode = CreateNode("First Node", firstNodePosition, 0, 2);
            var secondNode = CreateNode("Second Node", secondNodePosition, 2, 0);

            var startPort = firstNode.outputContainer[0] as Port;
            var endPort = secondNode.inputContainer[0] as Port;

            var edge = CreateEdge(startPort, endPort);

            yield return null;

            graphView.AddToSelection(edge);
            graphView.AddToSelection(secondNode);

            Assert.AreEqual(0.0, graphView.contentViewContainer.transform.position.x);
            Assert.AreEqual(0.0, graphView.contentViewContainer.transform.position.y);

            graphView.FrameSelection();

            Assert.LessOrEqual(graphView.contentViewContainer.transform.position.x, -firstNodePosition.x / 2);
            Assert.LessOrEqual(graphView.contentViewContainer.transform.position.y, -firstNodePosition.y / 2);
        }

        [Test]
        public void FrameNextPrevTest()
        {
            graphView.AddElement(new FooNode() {name = "N0"});
            graphView.AddElement(new FooNode() {name = "N1"});
            graphView.AddElement(new FooNode() {name = "N2"});
            graphView.AddElement(new FooNode() {name = "N3"});

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N1"));

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N2"));

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N3"));

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N0"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N3"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N2"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N1"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N0"));
        }

        [Test]
        public void FrameNextPrevWithoutSelectionTest()
        {
            graphView.AddElement(new FooNode() {name = "N0"});
            graphView.AddElement(new FooNode() {name = "N1"});
            graphView.AddElement(new FooNode() {name = "N2"});
            graphView.AddElement(new FooNode() {name = "N3"});

            // Reset selection for next test
            graphView.ClearSelection();

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N0"));

            // Reset selection for prev test
            graphView.ClearSelection();

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N3"));
        }

        [Test]
        public void FrameNextPrevWithNestedElementsTest()
        {
            StackNode stackNode;

            graphView.AddElement(new FooNode() { name = "N0" });
            graphView.AddElement(stackNode = new StackNode { name = "N1" });
            stackNode.AddElement(new FooNode() { name = "N2" });
            graphView.AddElement(new FooNode() { name = "N3" });

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N1"));

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N2"));

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N3"));

            graphView.FrameNext();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N0"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N3"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N2"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N1"));

            graphView.FramePrev();
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(GraphElement)));
            Assert.That(((GraphElement)graphView.selection[0]).name, Is.EqualTo("N0"));
        }

        [Test]
        public void FrameNextPrevPredicateTest()
        {
            graphView.AddElement(new FooNode() {name = "F0"});
            graphView.AddElement(new FooNode() {name = "F1"});
            graphView.AddElement(new BarNode() {name = "B0"});
            graphView.AddElement(new BarNode() {name = "B1"});
            graphView.AddElement(new FooNode() {name = "F2"});
            graphView.AddElement(new BarNode() {name = "B2"});

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext(x => x is FooNode);
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(FooNode)));
            Assert.That(((FooNode)graphView.selection[0]).name, Is.EqualTo("F1"));

            graphView.FrameNext(IsFooNode);
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(FooNode)));
            Assert.That(((FooNode)graphView.selection[0]).name, Is.EqualTo("F2"));

            graphView.FrameNext(x => x is FooNode);
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(FooNode)));
            Assert.That(((FooNode)graphView.selection[0]).name, Is.EqualTo("F0"));

            graphView.FramePrev(IsFooNode);
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(FooNode)));
            Assert.That(((FooNode)graphView.selection[0]).name, Is.EqualTo("F2"));

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext(x => x.name.Contains("0"));
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(Node)));
            Assert.That(((Node)graphView.selection[0]).name, Is.EqualTo("F0"));

            graphView.FrameNext(x => x.name.Contains("0"));
            Assert.That(graphView.selection.Count, Is.EqualTo(1));
            Assert.That(graphView.selection[0], Is.AssignableTo(typeof(Node)));
            Assert.That(((Node)graphView.selection[0]).name, Is.EqualTo("B0"));
        }

        private bool IsFooNode(GraphElement element)
        {
            return element is FooNode;
        }
    }
}
