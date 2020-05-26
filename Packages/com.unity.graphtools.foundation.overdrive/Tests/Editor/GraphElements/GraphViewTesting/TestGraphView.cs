using Unity.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphViewTestUtilities
{
    public class TestGraphView : GraphView
    {
        public readonly ContentDragger contentDragger;
        public readonly SelectionDragger selectionDragger;
        public readonly RectangleSelector rectangleSelector;
        public readonly FreehandSelector freehandSelector;

        public TestGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            contentDragger = new ContentDragger();
            selectionDragger = new SelectionDragger();
            rectangleSelector = new RectangleSelector();
            freehandSelector = new FreehandSelector();

            this.AddManipulator(contentDragger);
            this.AddManipulator(selectionDragger);
            this.AddManipulator(rectangleSelector);
            this.AddManipulator(freehandSelector);

            Insert(0, new GridBackground());

            focusable = true;
        }
    }
}
