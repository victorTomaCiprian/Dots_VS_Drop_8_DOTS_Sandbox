using System;
using System.Collections.Generic;
using System.Linq;
using DotsStencil;
using Unity.Mathematics;
using Unity.GraphElements;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine.UIElements;

namespace Elements
{
    public class DotsNode : HighLevelNode, IContextualMenuBuilder
    {
        public DotsNode(INodeModel model, Store store, GraphView graphView)
            : base(model, store, graphView) {}

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            // TODO find a proper solution - contextual inspectors ?
            var variableCountFields = model.GetType().GetFields()
                .Select(f => (Field: f, Attribute: (HackContextualMenuVariableCountAttribute)Attribute.GetCustomAttribute(f, typeof(HackContextualMenuVariableCountAttribute))))
                .Where(f => f.Attribute != null);
            foreach (var variableCountField in variableCountFields)
            {
                var fieldInfo = variableCountField.Field;
                if (fieldInfo.FieldType != typeof(int))
                    throw new InvalidOperationException("VariableCountAttribute is only supported on int fields");
                var portDesc = variableCountField.Attribute.Description;
                if (model is BaseDotsNodeModel dotsNodeModel
                    && dotsNodeModel.PortCountData.TryGetValue(fieldInfo.Name, out var customPortDesc))
                    portDesc = customPortDesc;
                var itemName = portDesc.Name ?? fieldInfo.Name;
                int max = portDesc.Max;
                evt.menu.AppendAction($"Add {itemName}", action: action =>
                {
                    fieldInfo.SetValue(model, (int)fieldInfo.GetValue(model) + 1);
                    ((NodeModel)model).DefineNode();
                    m_Store.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology, new List<IGraphElementModel> { model }));
                }, action =>
                    {
                        int value = (int)fieldInfo.GetValue(model);
                        return max == -1 || value < max ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    });
                int min = math.max(0, portDesc.Min);
                evt.menu.AppendAction($"Remove {itemName}", action: action =>
                {
                    fieldInfo.SetValue(model, Math.Max(0, (int)fieldInfo.GetValue(model) - 1));
                    ((NodeModel)model).DefineNode();
                    m_Store.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology, new List<IGraphElementModel> { model }));
                }, action =>
                    {
                        int value = (int)fieldInfo.GetValue(model);
                        return value > min ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    });
            }
        }
    }
}
