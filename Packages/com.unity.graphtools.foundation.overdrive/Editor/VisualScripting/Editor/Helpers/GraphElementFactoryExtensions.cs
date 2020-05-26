using System;
using Unity.GraphElements;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [GraphtoolsExtensionMethods]
    public static class GraphElementFactoryExtensions
    {
        public static GraphElement CreateFunction(this INodeBuilder builder, Store store, FunctionModel model)
        {
            var functionNode = new FunctionNode(store, model, builder);
            return functionNode;
        }

        public static GraphElement CreateGetComponent(this INodeBuilder builder, Store store, HighLevelNodeModel model)
        {
            var functionNode = new HighLevelNode(model, store, builder.GraphView);
            return functionNode;
        }

        public static GraphElement CreateStack(this INodeBuilder builder, Store store, StackBaseModel model)
        {
            return new StackNode(store, model, builder);
        }

        public static GraphElement CreateIfConditionNode(this INodeBuilder builder, Store store, IfConditionNodeModel model)
        {
            return new IfConditionNode(model, store, builder.GraphView);
        }

        public static GraphElement CreatePlacemat(this INodeBuilder builder, Store store, PlacematModel model)
        {
            return builder.GraphView.placematContainer.CreatePlacemat(() => new Placemat(model, store, builder.GraphView), model.Position, model.ZOrder, model.Title);
        }

        public static GraphElement CreateNode(this INodeBuilder builder, Store store, NodeModel model)
        {
            return new Node(model, store, builder.GraphView);
        }

        public static GraphElement CreateInlineExpressionNode(this INodeBuilder builder, Store store, InlineExpressionNodeModel model)
        {
            return new RenamableNode(model, store, builder.GraphView);
        }

        public static GraphElement CreateBinaryOperator(this INodeBuilder builder, Store store, BinaryOperatorNodeModel model)
        {
            return new Node(model, store, builder.GraphView)
            {
                CustomSearcherHandler = (node, nStore, pos, _) =>
                {
                    SearcherService.ShowEnumValues("Pick a new operator type", typeof(BinaryOperatorKind), pos, (pickedEnum, __) =>
                    {
                        if (pickedEnum != null)
                        {
                            ((BinaryOperatorNodeModel)node.model).Kind = (BinaryOperatorKind)pickedEnum;
                            nStore.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology));
                        }
                    });
                    return true;
                }
            };
        }

        public static GraphElement CreateUnaryOperator(this INodeBuilder builder, Store store, UnaryOperatorNodeModel model)
        {
            return new Node(model, store, builder.GraphView)
            {
                CustomSearcherHandler = (node, nStore, pos, _) =>
                {
                    SearcherService.ShowEnumValues("Pick a new operator type", typeof(UnaryOperatorKind), pos, (pickedEnum, __) =>
                    {
                        if (pickedEnum != null)
                        {
                            ((UnaryOperatorNodeModel)node.model).Kind = (UnaryOperatorKind)pickedEnum;
                            nStore.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology));
                        }
                    });
                    return true;
                }
            };
        }

        public static void GetTokenPorts(Store store, INodeModel model, out Port inputPort, out Port outputPort, Orientation? orientation = null)
        {
            inputPort = null;
            outputPort = null;

            // TODO: weirdly VariableNodeModels implement IHasMainOutputPort, but that 'output port' can be an input

            Port newPort = null;
            switch (model)
            {
                // Token only support one input port, we use the first one found.
                case IHasMainExecutionInputPort inputTriggerModel:
                    newPort = Port.Create(inputTriggerModel.ExecutionInputPort, store, GetPortOrientation(inputTriggerModel.ExecutionInputPort));
                    break;
                case IHasMainExecutionOutputPort outputTriggerModel:
                    newPort = Port.Create(outputTriggerModel.ExecutionOutputPort, store, GetPortOrientation(outputTriggerModel.ExecutionOutputPort));
                    break;
                case IHasMainInputPort inputModel:
                    newPort = Port.Create(inputModel.InputPort, store, GetPortOrientation(inputModel.InputPort));
                    break;
                case IHasMainOutputPort outputModel:
                    if (outputModel.OutputPort != null)
                        newPort = Port.Create(outputModel.OutputPort, store, GetPortOrientation(outputModel.OutputPort));
                    break;
            }

            if (newPort != null)
                SetupPort(newPort, ref inputPort, ref outputPort);

            void SetupPort(Port port, ref Port resultInputPort, ref Port resultOutputPort)
            {
                var className = port.direction == Direction.Input ? "left" : "right";
                port.AddToClassList(className);
                if (port.direction == Direction.Input)
                    resultInputPort = port;
                else
                    resultOutputPort = port;
            }

            Orientation GetPortOrientation(IPortModel port)
            {
                if (orientation != null)
                    return orientation.Value;
                if (port == null)
                    return Orientation.Horizontal;

                switch (port.PortType)
                {
                    case PortType.Data:
                    case PortType.Event:
                    case PortType.Instance:
                        return Orientation.Horizontal;
                    case PortType.Execution:
                        return Orientation.Vertical;
                    case PortType.Loop:
                        return port.Direction == Direction.Output ? Orientation.Vertical : Orientation.Horizontal;
                    default:
                        return Orientation.Horizontal;
                }
            }
        }

        public static GraphElement CreateToken(this INodeBuilder builder, Store store, IVariableModel model)
        {
            var isExposed = model.DeclarationModel?.IsExposed;
            Texture2D icon = (isExposed != null && isExposed.Value)
                ? VisualScriptingIconUtility.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png")
                : null;

            GetTokenPorts(store, model, out var input, out var output, Orientation.Horizontal);

            var token = new Token(model, store, input, output, builder.GraphView, icon);
            if (model.DeclarationModel != null && model.DeclarationModel is LoopVariableDeclarationModel loopVariableDeclarationModel)
                VseUtility.AddTokenIcon(token, loopVariableDeclarationModel.TitleComponentIcon);
            return token;
        }

        public static GraphElement CreateConstantToken(this INodeBuilder builder, Store store, IConstantNodeModel model)
        {
            GetTokenPorts(store, model, out var input, out var output);

            return new Token(model, store, input, output, builder.GraphView);
        }

        public static GraphElement CreateToken(this INodeBuilder builder, Store store, IStringWrapperConstantModel model)
        {
            return CreateConstantToken(builder, store, model);
        }

        public static GraphElement CreateToken(this INodeBuilder builder, Store store, SystemConstantNodeModel model)
        {
            GetTokenPorts(store, model, out var input, out var output);

            return new Token(model, store, input, output, builder.GraphView);
        }

        public static GraphElement CreateStickyNote(this INodeBuilder builder, Store store, StickyNoteModel model)
        {
            return new StickyNote(store, model, model.Position, builder.GraphView);
        }

        public static GraphElement CreateMacro(this INodeBuilder builder, Store store, MacroRefNodeModel model)
        {
            return new MacroNode(model, store, builder.GraphView);
        }

        public static GraphElement CreateEdgePortal(this INodeBuilder builder, Store store, IEdgePortalModel model)
        {
            GetTokenPorts(store, model, out var input, out var output, Orientation.Horizontal);

            return new Token(model, store, input, output, builder.GraphView);
        }
    }
}
