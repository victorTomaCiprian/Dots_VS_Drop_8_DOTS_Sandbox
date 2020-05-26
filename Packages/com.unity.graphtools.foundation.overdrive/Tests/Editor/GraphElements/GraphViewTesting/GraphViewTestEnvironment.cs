using UnityEngine;
using NUnit.Framework;
using Unity.GraphToolsFoundations.Bridge;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEditor.GraphViewTestUtilities
{
    [SetUpFixture]
    // Since GraphView tests rely on some global state related to UIElements mouse capture
    // Here we make sure to disable input events on the whole editor UI to avoid other interactions
    // from interfering with the tests being run
    public class GraphViewTestEnvironment
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            SetDisableInputEventsOnAllWindows(true);
            MouseCaptureController.ReleaseMouse();

            Debug.Assert(!GraphViewStaticBridge.GetDisableThrottling());
            GraphViewStaticBridge.SetDisableThrottling(true);
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            SetDisableInputEventsOnAllWindows(false);

            GraphViewStaticBridge.SetDisableThrottling(false);
        }

        static void SetDisableInputEventsOnAllWindows(bool value)
        {
            if (InternalEditorUtility.isHumanControllingUs == false)
                return;

            foreach (var otherWindow in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                otherWindow.SetDisableInputEvents(value);
            }
        }
    }
}
