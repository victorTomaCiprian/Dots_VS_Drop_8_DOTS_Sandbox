using System;
using System.Collections.Generic;
using System.Linq;
using Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;
using Port = Runtime.Port;
using ValueType = Runtime.ValueType;

namespace DotsStencil
{
    public class DotsTranslator : ITranslator
    {
        public class DotsCompilationResult : CompilationResult
        {
            public GraphDefinition GraphDefinition = new GraphDefinition();
        }

        public bool SupportsCompilation() => true;

        static bool TranslateNode(GraphBuilder builder, INodeModel nodeModel, out INode node,
            out Dictionary<string, uint> portToOffsetMapping, out uint? preAllocatedDataIndex)
        {
            Assert.IsNotNull(nodeModel);
            preAllocatedDataIndex = null;

            switch (nodeModel)
            {
                case SetVariableNodeModel setVariableNodeModel:
                {
                    node = setVariableNodeModel.Node;
                    portToOffsetMapping = setVariableNodeModel.PortToOffsetMapping;
                    if (setVariableNodeModel.DeclarationModel == null)
                    {
                        return false;
                    }
                    preAllocatedDataIndex = builder.GetVariableDataIndex(setVariableNodeModel.DeclarationModel).DataIndex;
                    return true;
                }
                case IEventNodeModel eventNodeModel:
                    node = eventNodeModel.Node;
                    ((IEventNode)node).EventId = TypeHash.CalculateStableTypeHash(
                        eventNodeModel.TypeHandle.Resolve(nodeModel.GraphModel.Stencil));
                    portToOffsetMapping = eventNodeModel.PortToOffsetMapping;
                    return true;

                case SubgraphReferenceNodeModel subgraphReferenceNodeModel:
                    node = subgraphReferenceNodeModel.Node;
                    portToOffsetMapping = subgraphReferenceNodeModel.PortToOffsetMapping;
                    return true;

                case IDotsNodeModel dotsNodeModel:
                    node = dotsNodeModel.Node;
                    portToOffsetMapping = dotsNodeModel.PortToOffsetMapping;
                    if (nodeModel is IReferenceComponentTypes referenceComponentTypes)
                    {
                        foreach (var typeReference in referenceComponentTypes.ReferencedTypes)
                        {
                            if (typeReference.TypeIndex != -1)
                            {
                                builder.AddReferencedComponent(typeReference);
                            }
                        }
                    }
                    return true;

                case IConstantNodeModel constantNodeModel:
                    HandleConstants(builder, out node, out portToOffsetMapping, constantNodeModel);
                    return true;

                case IVariableModel variableModel:
                    return HandleVariable(builder, out node, out portToOffsetMapping,
                        out preAllocatedDataIndex, variableModel);
                default:
                    throw new NotImplementedException(
                        $"Don't know how to translate a node of type {nodeModel.GetType()}: {nodeModel}");
            }
        }

        static bool HandleVariable(GraphBuilder builder, out INode node,
            out Dictionary<string, uint> portToOffsetMapping, out uint? preAllocatedDataIndex, IVariableModel variableModel)
        {
            if (variableModel.DeclarationModel.IsInputOrOutputTrigger())
            {
                preAllocatedDataIndex = null;
                portToOffsetMapping = new Dictionary<string, uint>();
                if (variableModel.DeclarationModel.Modifiers == ModifierFlags.ReadOnly) // Input
                {
                    var trigger = builder.DeclareInputTrigger(variableModel.DeclarationModel.VariableName);
                    node = MapPort(portToOffsetMapping, variableModel.OutputPort.UniqueId, ref trigger.Output.Port, trigger);
                }
                else
                {
                    var trigger = builder.DeclareOutputTrigger(variableModel.DeclarationModel.VariableName);
                    node = MapPort(portToOffsetMapping, variableModel.OutputPort.UniqueId, ref trigger.Input.Port, trigger);
                }
                return true;
            }

            var valueType = variableModel.DeclarationModel.DataType.TypeHandleToValueType();
            Assert.AreEqual(VariableType.GraphVariable, variableModel.DeclarationModel.VariableType);
            var type = GraphBuilder.GetVariableType(variableModel.DeclarationModel);
            switch (type)
            {
                case GraphBuilder.VariableType.ObjectReference:
                    switch (valueType)
                    {
                        case ValueType.Entity:
                            preAllocatedDataIndex = builder.GetVariableDataIndex(variableModel.DeclarationModel).DataIndex;
                            portToOffsetMapping = new Dictionary<string, uint>();
                            var cf = new ConstantEntity();
                            node = MapPort(portToOffsetMapping, variableModel.OutputPort.UniqueId, ref cf.ValuePort.Port, cf);
                            return true;
                    }
                    break;
                case GraphBuilder.VariableType.Variable: // Data
                    // Just create an edge later
                    node = default;
                    portToOffsetMapping = null;
                    preAllocatedDataIndex = null;
                    return false;
                case GraphBuilder.VariableType.InputOutput:
                    Assert.IsFalse(variableModel.DeclarationModel.IsDataOutput()); // TODO check legit
                    portToOffsetMapping = new Dictionary<string, uint>();
                    preAllocatedDataIndex = null;
                    var inputData = builder.DeclareInputData(variableModel.DeclarationModel.VariableName, variableModel.DeclarationModel.DataType.TypeHandleToValueType());
                    node = MapPort(portToOffsetMapping, variableModel.OutputPort.UniqueId, ref inputData.Output.Port, inputData);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("Variable type not supported: " + type);
            }

            throw new ArgumentOutOfRangeException(valueType.ToString());
        }

        static void HandleConstants(GraphBuilder builder, out INode node, out Dictionary<string, uint> portToOffsetMapping,
            IConstantNodeModel constantNodeModel)
        {
            portToOffsetMapping = new Dictionary<string, uint>();
            var outputPortId = constantNodeModel.OutputPort?.UniqueId ?? "";
            switch (constantNodeModel)
            {
                case StringConstantModel stringConstantModel:
                {
                    var index = builder.StoreStringConstant(stringConstantModel.value);
                    var cf = new ConstantString {Value = new StringReference(index)};
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case BooleanConstantNodeModel booleanConstantNodeModel:
                {
                    var cf = new ConstantBool { Value = booleanConstantNodeModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case IntConstantModel intConstantModel:
                {
                    var cf = new ConstantInt { Value = intConstantModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case FloatConstantModel floatConstantModel:
                {
                    var cf = new ConstantFloat { Value = floatConstantModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case Vector2ConstantModel vector2ConstantModel:
                {
                    var cf = new ConstantFloat2 { Value = vector2ConstantModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case Vector3ConstantModel vector3ConstantModel:
                {
                    var cf = new ConstantFloat3 { Value = vector3ConstantModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case Vector4ConstantModel vector4ConstantModel:
                {
                    var cf = new ConstantFloat4 { Value = vector4ConstantModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case QuaternionConstantModel quaternionConstantModel:
                {
                    var cf = new ConstantQuaternion { Value = quaternionConstantModel.value };
                    node = MapPort(portToOffsetMapping, outputPortId, ref cf.ValuePort.Port, cf);
                    return;
                }
                case ObjectConstantModel _:
                {
                    throw new NotImplementedException(
                        "Conversion and all - either a prefab (might live in a graph) or a scene object (must be injected during runtime bootstrap)");

                    // portToOffsetMapping = new Dictionary<IPortModel, uint>();
                    // var cf = new ConstantEntity {Value = objectConstantModel.value};
                    // MapPort(portToOffsetMapping, objectConstantModel.OutputPort, ref cf.ValuePort.Port, nodeId);
                    // node = cf;
                    // return;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        public static INode MapPort(Dictionary<string, uint> portToOffsetMapping, string portUniqueId, ref Port port, in INode node)
        {
            uint index = (uint)(portToOffsetMapping.Count + 1);
            portToOffsetMapping.Add(portUniqueId, index);
            port.Index = index;
            return node;
        }

        public CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType,
            CompilationOptions compilationOptions)
        {
            var builder = new GraphBuilder();

            // Pre-allocate data indices for graph variables
            foreach (var graphVariableModel in graphModel.GraphVariableModels)
            {
                // used inputs/outputs are created by their unique variableNodeModel in the graph. we'll create unused i/os later
                // this means duplicate variable I/O nodes will throw
                if (graphVariableModel.IsInputOrOutputTrigger())
                    continue;
                Value? initValue = GetValueFromConstant(graphVariableModel.InitializationModel);
                builder.DeclareVariable(graphVariableModel, GraphBuilder.GetVariableType(graphVariableModel), initValue);
            }

            foreach (var nodeModel in graphModel.NodeModels)
            {
                if (nodeModel.State == ModelState.Disabled || nodeModel is IEdgePortalModel)
                    continue;

                {
                    var nodeId = builder.GetNextNodeId();
                    if (TranslateNode(builder, nodeModel, out var rnode, out var mapping, out var preAllocatedDataIndex))
                    {
                        // TODO theor this lambda is broken. we need per port index. a SubgraphReferenceNodeModel has two input data ports (Target and DataInputs) so that won't work
                        bool used = false;
                        builder.AddNodeFromModel(nodeModel, nodeId, rnode, mapping, _ =>
                        {
                            if (preAllocatedDataIndex.HasValue)
                            {
                                Unity.Assertions.Assert.IsFalse(used, "Preallocated data index used multiple times for the same node: " + nodeModel);
                                used = true;
                            }

                            return preAllocatedDataIndex;
                        });
                        switch (nodeModel)
                        {
                            // TODO not pretty
                            case SmartObjectReferenceNodeModel smartObjectReferenceNodeModel:
                            {
                                uint targetEntityPreAllocatedDataIndex = builder.GetVariableDataIndex(smartObjectReferenceNodeModel.DeclarationModel).DataIndex;
                                builder.BindVariableToInput(new GraphBuilder.VariableHandle(targetEntityPreAllocatedDataIndex), ((GraphReference)rnode).Target);
                                break;
                            }
                            case SubgraphReferenceNodeModel subgraphReferenceNodeModel:
                            {
                                var targetIndex = builder.AllocateDataIndex();
                                builder.BindVariableToInput(new GraphBuilder.VariableHandle(targetIndex), ((GraphReference)rnode).Target);
                                builder.BindSubgraph(targetIndex, subgraphReferenceNodeModel.GraphReference);
                                break;
                            }
                        }
                    }
                }

                // create a node and an edge for each embedded constant
                foreach (var portModel in nodeModel.InputsByDisplayOrder)
                {
                    if (portModel.EmbeddedValue == null || portModel.Connected)
                        continue;
                    var embeddedNodeId = builder.GetNextNodeId();
                    if (!TranslateNode(builder, portModel.EmbeddedValue, out var embeddedNode, out var embeddedPortMapping, out var embeddedPreAllocatedDataIndex))
                        continue;
                    builder.AddNodeFromModel(portModel.EmbeddedValue, embeddedNodeId, embeddedNode, embeddedPortMapping, _ => embeddedPreAllocatedDataIndex);
                    builder.CreateEdge(portModel.EmbeddedValue, string.Empty, nodeModel, portModel.UniqueId);
                }
            }

            foreach (var graphVariableModel in graphModel.GraphVariableModels)
            {
                // unused i/os only
                if (graphVariableModel.IsInputOrOutputTrigger())
                {
                    if (graphVariableModel.Modifiers == ModifierFlags.ReadOnly) // Input
                    {
                        if (!builder.GetExistingInputTrigger(graphVariableModel.VariableName, out _))
                            builder.AddNode(builder.DeclareInputTrigger(graphVariableModel.VariableName));
                    }
                    else
                    {
                        if (!builder.GetExistingOutputTrigger(graphVariableModel.VariableName, out _))
                            builder.AddNode(builder.DeclareOutputTrigger(graphVariableModel.VariableName));
                    }
                }
            }

            EvaluateEdgePortals<ExecutionEdgePortalEntryModel, ExecutionEdgePortalExitModel>();
            EvaluateEdgePortals<DataEdgePortalEntryModel, DataEdgePortalExitModel>();

            foreach (var edgeModel in graphModel.EdgeModels)
            {
                if (edgeModel?.OutputPortModel == null || edgeModel.InputPortModel == null)
                    continue;
                if (edgeModel.OutputPortModel.NodeModel is IEdgePortalModel || edgeModel.InputPortModel.NodeModel is IEdgePortalModel)
                    continue;
                if (edgeModel.OutputPortModel.NodeModel.State == ModelState.Disabled || edgeModel.InputPortModel.NodeModel.State == ModelState.Disabled)
                    continue;
                builder.CreateEdge(edgeModel.OutputPortModel, edgeModel.InputPortModel);
            }

            var stencil = ((DotsStencil)graphModel.Stencil);
            var result = builder.Build(stencil);
            var graphModelAssetModel = graphModel.AssetModel as Object;
            if (graphModelAssetModel)
            {
                if (!stencil.CompiledScriptingGraphAsset)
                {
                    stencil.CompiledScriptingGraphAsset = ScriptableObject.CreateInstance<ScriptingGraphAsset>();
                }

                stencil.CompiledScriptingGraphAsset.Definition = result.GraphDefinition;

                builder.CreateDebugSymbols(stencil);

                Utility.SaveAssetIntoObject(stencil.CompiledScriptingGraphAsset, graphModelAssetModel);
            }

            return result;

            void EvaluateEdgePortals<T1, T2>()
                where T1 : IEdgePortalEntryModel
                where T2 : IEdgePortalExitModel
            {
                var portalEntries = graphModel.NodeModels.OfType<T1>().GroupBy(p => p.PortalID).ToDictionary(x => x.Key, x => x.OrderBy(p => p.EvaluationOrder).ToList());
                var portalExits = graphModel.NodeModels.OfType<T2>().GroupBy(p => p.PortalID).ToDictionary(x => x.Key, x => x.OrderBy(p => p.EvaluationOrder).ToList());

                foreach (var portalIDs in portalEntries.Keys)
                {
                    if (!portalExits.TryGetValue(portalIDs, out var exits))
                        continue;

                    foreach (var outputPort in portalEntries[portalIDs].SelectMany(p => p.InputPort.ConnectionPortModels))
                    {
                        foreach (var inputPort in exits.SelectMany(p => p.OutputPort.ConnectionPortModels))
                        {
                            builder.CreateEdge(outputPort, inputPort);
                        }
                    }
                }
            }
        }

        Value? GetValueFromConstant(IConstantNodeModel initializationModel)
        {
            if (initializationModel == null)
                return null;
            switch (initializationModel.Type.GenerateTypeHandle(initializationModel.GraphModel.Stencil).TypeHandleToValueType())
            {
                case ValueType.Bool:
                    return (bool)initializationModel.ObjectValue;
                case ValueType.Int:
                    return (int)initializationModel.ObjectValue;
                case ValueType.Float:
                    return (float)initializationModel.ObjectValue;
                case ValueType.Float2:
                    return (float2)(Vector2)initializationModel.ObjectValue;
                case ValueType.Float3:
                    return (float3)(Vector3)initializationModel.ObjectValue;
                case ValueType.Float4:
                    return (float4)(Vector4)initializationModel.ObjectValue;
                case ValueType.Quaternion:
                    return (quaternion)(Quaternion)initializationModel.ObjectValue;
                // case ValueType.Entity:
                // return (Entity)initializationModel.ObjectValue;
                // case ValueType.StringReference:
                // return (StringReference)initializationModel.ObjectValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class ValueExtensions
    {
        static TypeHandle s_EntityTypeHandle = new TypeHandle(typeof(Entity).AssemblyQualifiedName);
        public static TypeHandle ValueTypeToTypeHandle(this ValueType valueType)
        {
            switch (valueType)
            {
                case ValueType.Unknown:
                    return TypeHandle.Unknown;
                case ValueType.Bool:
                    return TypeHandle.Bool;
                case ValueType.Int:
                    return TypeHandle.Int;
                case ValueType.Float:
                    return TypeHandle.Float;
                case ValueType.Float2:
                    return TypeHandle.Vector2;
                case ValueType.Float3:
                    return TypeHandle.Vector3;
                case ValueType.Float4:
                    return TypeHandle.Vector4;
                case ValueType.Quaternion:
                    return TypeHandle.Quaternion;
                case ValueType.Entity:
                    return s_EntityTypeHandle;
                case ValueType.StringReference:
                    return TypeHandle.String;
                default:
                    throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null);
            }
        }

        public static ValueType TypeHandleToValueType(this TypeHandle handle)
        {
            if (handle.TryTypeHandleToValueType(out var typeHandleToValueType))
                return typeHandleToValueType;
            throw new ArgumentOutOfRangeException(nameof(handle), "Unknown typehandle");
        }

        public static ValueType TypeHandleToValueTypeOrUnknown(this TypeHandle handle)
        {
            if (handle.TryTypeHandleToValueType(out var typeHandleToValueType))
                return typeHandleToValueType;
            return ValueType.Unknown;
        }

        public static bool TryTypeHandleToValueType(this TypeHandle handle, out ValueType typeHandleToValueType)
        {
            typeHandleToValueType = ValueType.Unknown;
            if (handle == TypeHandle.Unknown)
            {
                typeHandleToValueType = ValueType.Unknown;
                return true;
            }

            if (handle == TypeHandle.Bool)
            {
                typeHandleToValueType = ValueType.Bool;
                return true;
            }

            if (handle == TypeHandle.Int)
            {
                typeHandleToValueType = ValueType.Int;
                return true;
            }

            if (handle == TypeHandle.Float)
            {
                typeHandleToValueType = ValueType.Float;
                return true;
            }

            if (handle == TypeHandle.Vector2)
            {
                typeHandleToValueType = ValueType.Float2;
                return true;
            }

            if (handle == TypeHandle.Vector3)
            {
                typeHandleToValueType = ValueType.Float3;
                return true;
            }

            if (handle == TypeHandle.Vector4)
            {
                typeHandleToValueType = ValueType.Float4;
                return true;
            }

            if (handle == TypeHandle.Quaternion)
            {
                typeHandleToValueType = ValueType.Quaternion;
                return true;
            }

            if (handle == s_EntityTypeHandle)
            {
                typeHandleToValueType = ValueType.Entity;
                return true;
            }

            if (handle == TypeHandle.GameObject)
            {
                typeHandleToValueType = ValueType.Entity;
                return true;
            }

            if (handle == TypeHandle.String)
            {
                typeHandleToValueType = ValueType.StringReference;
                return true;
            }

            return false;
        }
    }
}
