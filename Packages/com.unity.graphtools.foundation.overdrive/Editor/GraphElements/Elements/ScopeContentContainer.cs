using System.Collections.Generic;
using Unity.GraphToolsFoundations.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphElements
{
    internal class ScopeContentContainer : VisualElement
    {
        public IEnumerable<GraphElement> containedElements { get; set; }

        public Rect contentRectInViewportSpace { get; private set; }

        public ScopeContentContainer()
        {
            this.SetRequireMeasureFunction();
        }

        protected override Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            GraphView graphView = GetFirstAncestorOfType<GraphView>();
            VisualElement viewport = graphView.contentViewContainer;

            contentRectInViewportSpace = Rect.zero;

            // Compute the bounding box of the content of the scope in viewport space (because nodes are not parented by the scope that contains them)
            foreach (GraphElement subElement in containedElements)
            {
                if (subElement.panel != panel)
                    continue;
                if (subElement.parent == null)
                    continue;

                Rect boundingRect = subElement.GetPosition();

                if (Scope.IsValidRect(boundingRect))
                {
                    boundingRect = subElement.parent.ChangeCoordinatesTo(viewport, boundingRect);

                    // Use the first element with a valid geometry as reference to compute the bounding box of contained elements
                    if (!Scope.IsValidRect(contentRectInViewportSpace))
                    {
                        contentRectInViewportSpace = boundingRect;
                    }
                    else
                    {
                        contentRectInViewportSpace = RectUtils.Encompass(contentRectInViewportSpace, boundingRect);
                    }
                }
            }

            return new Vector2(contentRectInViewportSpace.width, contentRectInViewportSpace.height);
        }
    }
}
