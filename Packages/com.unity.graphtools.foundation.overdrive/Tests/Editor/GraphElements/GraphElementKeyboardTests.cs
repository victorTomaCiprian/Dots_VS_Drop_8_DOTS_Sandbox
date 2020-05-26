using System.Collections;
using NUnit.Framework;
using Unity.GraphElements;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphViewTestUtilities;
using UnityEngine;

namespace Unity.GraphElementsTests
{
    public class GraphElementKeyboardTests : GraphViewTester
    {
        Node m_Node1;
        Node m_Node2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Rect(20, 0, 50, 50));
            m_Node2 = CreateNode("Node 2", new Rect(50, 30, 100, 100));
        }

        [UnityTest]
        public IEnumerator ShortcutsWork()
        {
            yield return null;

            // TODO Do we really want to validate that pressing some key did the "right thing" by checking
            // transformation matrices? Seems it's not testing the right thing in a "keyboard tests". I'd
            // rather check that pressing "A" (for example) invokes GraphView.FrameAll. The check that "FrameAll"
            // pans and zooms as expected shoudld be the suibject of another test suite that isn't keyboard related.
            // (and that could actually be more "real" unit tests, not requiring inputs of any kind).

            VisualElement vc = graphView.contentViewContainer;
            Matrix4x4 transform = vc.transform.matrix;

            Assert.AreEqual(Matrix4x4.identity, vc.transform.matrix);

            // Select first element
            Vector3 originalNode1GlobalCenter = m_Node1.GetGlobalCenter();
            helpers.MouseClickEvent(originalNode1GlobalCenter);
            yield return null;

            Assert.True(m_Node1.selected);
            Assert.False(m_Node2.selected);

            // Frame selection
            bool frameSelectedCommandIsValid = helpers.ValidateCommand("FrameSelected");
            yield return null;

            Assert.True(frameSelectedCommandIsValid);
            helpers.ExecuteCommand("FrameSelected");
            yield return null;

            transform *= Matrix4x4.Translate(m_Node1.GetGlobalCenter() - originalNode1GlobalCenter);
            Assert.AreEqual(transform, vc.transform.matrix);

            // Frame all
            helpers.KeyPressed(KeyCode.A);
            yield return null;

            transform *= Matrix4x4.Translate(new Vector3(-40, -40, 0));
            Assert.AreEqual(transform, vc.transform.matrix);

            // Reset view to origin
            window.SendEvent(new Event {type = EventType.KeyDown, character = 'o', keyCode = (KeyCode)'o'});
            yield return null;

            window.SendEvent(new Event {type = EventType.KeyUp, character = 'o', keyCode = (KeyCode)'o'});
            yield return null;

            Assert.AreEqual(Matrix4x4.identity, vc.transform.matrix);

            // Select next
            window.SendEvent(new Event {type = EventType.KeyDown, character = ']', keyCode = (KeyCode)']' });
            yield return null;

            window.SendEvent(new Event {type = EventType.KeyUp, character = ']', keyCode = (KeyCode)']' });
            yield return null;

            Assert.False(m_Node1.selected);
            Assert.True(m_Node2.selected);

            yield return null;
        }
    }
}
