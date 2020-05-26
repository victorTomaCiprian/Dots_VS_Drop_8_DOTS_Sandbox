using System.Collections;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphViewSelectionPersistenceTests : GraphViewTester
    {
        public GraphViewSelectionPersistenceTests() : base(enablePersistence: true) {}

        const string key1 = "node1";
        const string key2 = "node2";
        const string key3 = "node3";

        Node node1;
        Node node2;
        Node node3;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // When using the EnterPlayMode yield instruction, the SetUp() of the test is ran again
            // In this case, we skip this to be in control of when nodes are created
            if (EditorApplication.isPlaying)
                return;

            node1 = CreateNode(key1, new Vector2(200, 200), viewDataKey: key1);
            node2 = CreateNode(key2, new Vector2(400, 400), viewDataKey: key2);
            node3 = CreateNode(key3, new Vector2(600, 600), viewDataKey: key3);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            Undo.ClearAll();
        }

        [UnityTest]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesAfterPersistence()
        {
            // Add two nodes to selection.
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            node1 = CreateNode(key1, new Vector2(200, 200), viewDataKey: key1);
            node2 = CreateNode(key2, new Vector2(400, 400), viewDataKey: key2);
            node3 = CreateNode(key3, new Vector2(600, 600), viewDataKey: key3);

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);
        }

        [UnityTest]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesBeforePersistence()
        {
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            node1 = CreateNode(key1, new Vector2(200, 200), viewDataKey: key1);
            node2 = CreateNode(key2, new Vector2(400, 400), viewDataKey: key2);
            node3 = CreateNode(key3, new Vector2(600, 600), viewDataKey: key3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);
        }

        [UnityTest]
        public IEnumerator CanUndoSelection()
        {
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Undo.PerformUndo();

            Assert.False(node1.selected);
            Assert.False(node2.selected);
            Assert.False(node3.selected);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CanRedoSelection()
        {
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CanRedoSelectionAndEnterPlayMode()
        {
            // Note: this somewhat complex use case ensure that selection for redo
            // and persisted selection are kep in sync

            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);

            // Allow 1 frame to let the persistence be saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            node1 = CreateNode(key1, new Vector2(200, 200), viewDataKey: key1);
            node2 = CreateNode(key2, new Vector2(400, 400), viewDataKey: key2);
            node3 = CreateNode(key3, new Vector2(600, 600), viewDataKey: key3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);
        }

        [UnityTest]
        public IEnumerator StackSelectionIsRestoredWhenEnteringPlaymode_AddNodesAfterPersistence()
        {
            var stack = new StackNode();
            graphView.AddElement(stack);

            stack.AddElement(node1);
            stack.AddElement(node2);
            stack.AddElement(node3);

            // Add two nodes to selection.
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            node1 = CreateNodeWithoutAddingToGraphView(key1, new Vector2(200, 200), viewDataKey: key1);
            node2 = CreateNodeWithoutAddingToGraphView(key2, new Vector2(400, 400), viewDataKey: key2);
            node3 = CreateNodeWithoutAddingToGraphView(key3, new Vector2(600, 600), viewDataKey: key3);

            stack = new StackNode();

            stack.AddElement(node1);
            stack.AddElement(node2);
            stack.AddElement(node3);

            graphView.AddElement(stack);

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);
        }

        [UnityTest]
        public IEnumerator GroupSelectionIsRestoredWhenEnteringPlaymode_AddNodesAfterPersistence()
        {
            var group = new Group();
            graphView.AddElement(group);

            group.AddElement(node1);
            group.AddElement(node2);
            group.AddElement(node3);

            // Add two nodes to selection.
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            node1 = CreateNodeWithoutAddingToGraphView(key1, new Vector2(200, 200), viewDataKey: key1);
            node2 = CreateNodeWithoutAddingToGraphView(key2, new Vector2(400, 400), viewDataKey: key2);
            node3 = CreateNodeWithoutAddingToGraphView(key3, new Vector2(600, 600), viewDataKey: key3);

            group = new Group();

            group.AddElement(node1);
            group.AddElement(node2);
            group.AddElement(node3);

            graphView.AddElement(group);

            graphView.AddElement(node1);
            graphView.AddElement(node2);
            graphView.AddElement(node3);

            Assert.True(node1.selected);
            Assert.False(node2.selected);
            Assert.True(node3.selected);
        }

        [UnityTest]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsBeforeAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard();

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField() { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                graphView.AddToSelection(field);
                Assert.True(field.selected);
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add field to blackboard first then add blackboard to graphview.
                var blackboard = new Blackboard();

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField() { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                Assert.True(field.selected);
            }
        }

        [UnityTest]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsAfterAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard();

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField() { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                graphView.AddToSelection(field);
                Assert.True(field.selected);
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add blackboard to graphview first then add field to blackboard.
                var blackboard = new Blackboard();
                graphView.AddElement(blackboard);

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField() { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                Assert.True(field.selected);
            }
        }
    }
}
