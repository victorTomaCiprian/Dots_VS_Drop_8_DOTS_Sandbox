using System.Collections.Generic;
using Elements;
using Unity.GraphElements;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace DotsStencil
{
    [GraphtoolsExtensionMethods]
    static class DotsGraphElementFactoryExtensions
    {
        public static GraphElement CreateSmartObject(this INodeBuilder builder, Store store, SmartObjectReferenceNodeModel model)
        {
            return new SmartObjectReferenceNode(model, store, builder.GraphView);
        }

        public static GraphElement CreateSmartObject(this INodeBuilder builder, Store store, SubgraphReferenceNodeModel model)
        {
            return new SmartObjectReferenceNode(model, store, builder.GraphView);
        }

        public static GraphElement CreateSetVar(this INodeBuilder builder, Store store, SetVariableNodeModel model)
        {
            if (model.DeclarationModel.IsObjectReference())
                return GraphElementFactoryExtensions.CreateToken(builder, store, model);
            if (!model.IsGetter)
                return new SetVariableNode(model, store, builder.GraphView);

            GraphElementFactoryExtensions.GetTokenPorts(store, model, out var input, out var output);
            if (output != null)
                output.portName = "";

            var token = new DotsVariableToken(model, store, input, output, builder.GraphView);
            return token;
        }

        public static GraphElement CreateDotsNode(this INodeBuilder builder, Store store, BaseDotsNodeModel model)
        {
            var functionNode = new DotsNode(model, store, builder.GraphView);
            return functionNode;
        }
    }
}
