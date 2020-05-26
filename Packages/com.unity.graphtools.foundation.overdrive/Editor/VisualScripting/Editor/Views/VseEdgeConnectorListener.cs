using System;
using Unity.GraphElements;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    class VseEdgeConnectorListener : IEdgeConnectorListener
    {
        readonly Action<Unity.GraphElements.Edge, Vector2> m_OnDropOutsideDelegate;
        readonly Action<Unity.GraphElements.Edge> m_OnDropDelegate;

        public VseEdgeConnectorListener(Action<Unity.GraphElements.Edge, Vector2> onDropOutsideDelegate, Action<Unity.GraphElements.Edge> onDropDelegate)
        {
            m_OnDropOutsideDelegate = onDropOutsideDelegate;
            m_OnDropDelegate = onDropDelegate;
        }

        public void OnDropOutsidePort(Unity.GraphElements.Edge edge, Vector2 position)
        {
            m_OnDropOutsideDelegate(edge, position);
        }

        public void OnDrop(GraphView graphView, Unity.GraphElements.Edge edge)
        {
            m_OnDropDelegate(edge);
        }
    }
}
