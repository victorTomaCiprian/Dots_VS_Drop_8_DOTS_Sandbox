using Unity.GraphElements;
using UnityEditor.VisualScripting.Editor;
using UnityEngine.UIElements;
using Node = UnityEditor.VisualScripting.Editor.Node;

namespace DotsStencil
{
    class SmartObjectReferenceNode : Node
    {
        public SmartObjectReferenceNode(IGraphReferenceNodeModel model, Store store, GraphView graphView, string file = "Node.uxml")
            : base(model, store, graphView, file)
        {
            if (model.NeedsUpdate())
                RedefineNode();

            var clickable = new Clickable(DoAction);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter {button = MouseButton.LeftMouse, clickCount = 2});
            this.AddManipulator(clickable);
        }

        void DoAction()
        {
            var graphReferenceNodeModel = (IGraphReferenceNodeModel)model;
            if (graphReferenceNodeModel.GraphReference != null)
                m_Store.Dispatch(new LoadGraphAssetAction(
                    graphReferenceNodeModel.GraphReference.GraphModel.GetAssetPath(), graphReferenceNodeModel.GetBoundObject(m_Store.GetState().EditorDataModel), true, LoadGraphAssetAction.Type.PushOnStack));
        }
    }
}
