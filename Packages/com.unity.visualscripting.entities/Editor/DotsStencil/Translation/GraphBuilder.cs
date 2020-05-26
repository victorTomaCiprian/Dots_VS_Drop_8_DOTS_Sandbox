using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DotsStencil;
using Runtime;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using VisualScripting.Editor;
using Port = Runtime.Port;
using PortType = Runtime.PortType;
using SetVariableNodeModel = DotsStencil.SetVariableNodeModel;
using ValueType = Runtime.ValueType;

public class GraphBuilder
{
    internal struct MappedNode
    {
        public uint FirstPortIndex;
        public NodeId NodeId;
    }

    const string k_GraphDumpMenu = "Visual Scripting/Dump Graph";

    static bool s_IsGraphDumpEnabled;

    Dictionary<INodeModel, MappedNode> m_NodeMapping;
    TranslationSetupContext m_Ctx;
    Dictionary<INodeModel, Dictionary<string, uint>> m_PortOffsetMappings;
    List<(uint outputPortIndex, uint inputPortIndex)> m_EdgeTable;
    Dictionary<BindingId, VariableHandle> m_VariableToDataIndex;
    HashSet<TypeReference> m_ReferencedComponentTypeIndices;
    StringBuilder m_GraphDump;
    DotsTranslator.DotsCompilationResult m_Result;

    public GraphBuilder()
    {
        m_Result = new DotsTranslator.DotsCompilationResult();
        m_NodeMapping = new Dictionary<INodeModel, MappedNode>();
        m_Ctx = new TranslationSetupContext();
        m_PortOffsetMappings = new Dictionary<INodeModel, Dictionary<string, uint>>();
        m_EdgeTable = new List<(uint, uint)>();
        m_VariableToDataIndex = new Dictionary<BindingId, VariableHandle>();
        m_GraphDump = new StringBuilder();
        m_ReferencedComponentTypeIndices = new HashSet<TypeReference>();

        m_Result.GraphDefinition.PortInfoTable.Add(new PortInfo { NodeId = NodeId.Null, PortName = "NULL" });
        m_Result.GraphDefinition.DataPortTable.Add(NodeId.Null);
    }

    public NodeId GetNextNodeId()
    {
        return new NodeId((uint)(m_Result.GraphDefinition.NodeTable.Count));
    }

    public uint AllocateDataIndex()
    {
        var index = (uint)m_Result.GraphDefinition.DataPortTable.Count;
        m_Result.GraphDefinition.DataPortTable.Add(NodeId.Null);
        return index;
    }

    public VariableHandle GetVariableDataIndex(IVariableDeclarationModel variableModelDeclarationModel)
    {
        return m_VariableToDataIndex[GetBindingId(variableModelDeclarationModel)];
    }

    public enum VariableType
    {
        ObjectReference,
        Variable,
        InputOutput,
        SmartObject
    }

    public void DeclareVariable(IVariableDeclarationModel graphVariableModel, VariableType type, Value? initValue = null)
    {
        Assert.IsFalse(graphVariableModel.IsInputOrOutputTrigger());
        var bindingId = GetBindingId(graphVariableModel);
        uint dataIndex;
        if (type == VariableType.ObjectReference || type == VariableType.SmartObject)
        {
            Assert.IsFalse(initValue.HasValue);
            dataIndex = DeclareObjectReferenceVariable(bindingId).DataIndex;
        }
        else
        {
            dataIndex = AllocateDataIndex();

            if (initValue.HasValue)
            {
                m_Result.GraphDefinition.VariableInitValues.Add(new GraphDefinition.VariableInitValue { DataIndex = dataIndex, Value = initValue.Value });
            }
            var variableHandle = new VariableHandle(dataIndex);

            m_VariableToDataIndex.Add(bindingId, variableHandle);

            if (type == VariableType.InputOutput && graphVariableModel.IsDataOutput())
            {
                DeclareOutputData(bindingId, graphVariableModel.DataType.TypeHandleToValueType(), variableHandle);
            }
        }
    }

    public VariableHandle DeclareObjectReferenceVariable(BindingId bindingId)
    {
        var variableHandle = BindVariableToDataIndex(bindingId);
        m_Result.GraphDefinition.Bindings.Add(new GraphDefinition.InputBinding { Id = bindingId, DataIndex = variableHandle.DataIndex });
        return variableHandle;
    }

    public struct VariableHandle
    {
        public uint DataIndex;

        public VariableHandle(uint dataIndex)
        {
            DataIndex = dataIndex;
        }
    }

    public VariableHandle BindVariableToDataIndex(BindingId variableId)
    {
        uint dataIndex = AllocateDataIndex();
        m_VariableToDataIndex.Add(variableId, new VariableHandle(dataIndex));
        return new VariableHandle { DataIndex = dataIndex };
    }

    public static BindingId GetBindingId(IVariableDeclarationModel graphVariableModel)
    {
        var strGuid = graphVariableModel.GetId();
        GUID.TryParse(strGuid.Replace("-", null), out var guid);
        SerializableGUID serializableGUID = guid;
        serializableGUID.ToParts(out var p1, out var p2);
        BindingId id = BindingId.From(p1, p2);
        return id;
    }

    public int StoreStringConstant(string value)
    {
        var index = m_Result.GraphDefinition.Strings.FindIndex(s => s == value);
        if (index == -1)
        {
            index = m_Result.GraphDefinition.Strings.Count;
            m_Result.GraphDefinition.Strings.Add(value);
        }

        return index;
    }

    public void AddNodeFromModel(INodeModel nodeModel, NodeId nodeId, INode node, Dictionary<string, uint> portToOffsetMapping, Func<IPort, uint?> getOutputDataPortPreAllocatedDataIndex)
    {
        if (portToOffsetMapping == null)
            portToOffsetMapping = new Dictionary<string, uint>();
        m_GraphDump?.AppendLine($"  Node GUID: {nodeModel.Guid} Name: {nodeModel.Title}:\r\n" + String.Join("\r\n",
            portToOffsetMapping.Select(x =>
                $"    Name: {x.Key} / Value: {x.Value} / PortIndex: {m_Ctx.LastPortIndex + x.Value}")));

        // things to set up here: portOffsetMappings, nodeMapping + AddNode everything
        var lastPortIndex = m_Ctx.LastPortIndex;
        AddNodeInternal(nodeId, node, getOutputDataPortPreAllocatedDataIndex);
        m_PortOffsetMappings.Add(nodeModel, portToOffsetMapping);
        m_NodeMapping.Add(nodeModel, new MappedNode { NodeId = nodeId, FirstPortIndex = lastPortIndex });
    }

    public T AddNode<T>(T onUpdate, Func<IPort, uint?> getOutputDataPortPreAllocatedDataIndex = null) where T : struct, INode
    {
        return (T)AddNodeInternal(GetNextNodeId(), onUpdate, getOutputDataPortPreAllocatedDataIndex);
    }

    INode AddNodeInternal(NodeId id, INode node, Func<IPort, uint?> getOutputDataPortPreAllocatedDataIndex)
    {
        // For each port, bake its indices
        foreach (var fieldInfo in BaseDotsNodeModel.GetNodePorts(node.GetType()))
        {
            var innerPort = m_Ctx.SetupPort(node, fieldInfo, out var direction, out var portType, out var portName);

            for (int i = 0; i < innerPort.GetDataCount(); i++)
            {
                uint dataIndex = 0;
                if (portType == PortType.Data && direction == PortDirection.Output)
                {
                    dataIndex = getOutputDataPortPreAllocatedDataIndex?.Invoke(innerPort) ?? AllocateDataIndex();

                    // store port default values in graph definition
                    var metadata = BaseDotsNodeModel.GetPortMetadata(fieldInfo);
                    if (metadata.DefaultValue != null)
                    {
                        m_Result.GraphDefinition.VariableInitValues.Add(new GraphDefinition.VariableInitValue
                        {
                            DataIndex = dataIndex,
                            Value = ValueFromTypeAndObject(fieldInfo, metadata.DefaultValue)
                        });
                    }
                }

                bool isDataPort = portType == PortType.Data;
                bool isOutputPort = direction == PortDirection.Output;
                var newPortInfo = new PortInfo { IsDataPort = isDataPort, IsOutputPort = isOutputPort, DataIndex = dataIndex, NodeId = id, PortName = portName };
                m_Result.GraphDefinition.PortInfoTable.Add(newPortInfo);
            }
        }

        // Add the node to the defintion
        m_Result.GraphDefinition.NodeTable.Add(node);
        return node;
    }

    Value ValueFromTypeAndObject(FieldInfo fieldInfo, object value)
    {
        var metadata = BaseDotsNodeModel.GetPortMetadata(fieldInfo);
        switch (metadata.Type)
        {
            case ValueType.Bool:
                return (bool)value;
            case ValueType.Int:
                return (int)value;
            case ValueType.Float:
            case ValueType.Float2: // cannot put a float2/3/4 in an attribute
            case ValueType.Float3:
            case ValueType.Float4:
                return (float)value;
            case ValueType.StringReference:
                return new Value {StringReference = new StringReference(StoreStringConstant((string)value))};
            default:
                throw new ArgumentOutOfRangeException(metadata.Type.ToString());
        }
    }

    public void CreateEdge(OutputTriggerPort outputPortModel, InputTriggerPort inputPortModel) => CreateEdge(outputPortModel.Port.Index, inputPortModel.Port.Index);

    public void CreateEdge(OutputDataPort outputPortModel, InputDataPort inputPortModel) => CreateEdge(outputPortModel.Port.Index, inputPortModel.Port.Index);

    public void CreateEdge(IPortModel outputPortModel, IPortModel inputPortModel)
    {
        var outputNode = outputPortModel.NodeModel;
        var outputPortUniqueId = outputPortModel.UniqueId;
        var inputNode = inputPortModel.NodeModel;
        var inputPortUniqueId = inputPortModel.UniqueId;

        CreateEdge(outputNode, outputPortUniqueId, inputNode, inputPortUniqueId);
    }

    public void CreateEdge(INodeModel outputNode, string outputPortUniqueId, INodeModel inputNode, string inputPortUniqueId)
    {
        if (outputNode.State == ModelState.Disabled)
            return;
        if (inputNode.State == ModelState.Disabled)
            return;

        if (GetPortIndex(inputNode, inputPortUniqueId, out var inputPortIndex))
        {
            if (outputNode is VariableNodeModel variableNodeModel)
            {
                if (GetVariableType(variableNodeModel.DeclarationModel) == VariableType.Variable)
                {
                    if (!(variableNodeModel is SetVariableNodeModel setVariableNodeModel) || setVariableNodeModel.IsGetter)
                    {
                        var varIndex = GetVariableDataIndex(variableNodeModel.DeclarationModel);
                        BindVariableToInput(varIndex, new InputDataPort { Port = new Port { Index = inputPortIndex } });
                        return;
                    }
                }
            }

            if (GetPortIndex(outputNode, outputPortUniqueId, out var outputPortIndex))
            {
                CreateEdge(outputPortIndex, inputPortIndex);

                m_GraphDump?.AppendLine(
                    $"  {outputPortUniqueId}:{outputPortIndex} -> {inputPortUniqueId}:{inputPortIndex}");
            }
        }

        bool GetPortIndex(INodeModel nodeMode, string portUniqueId, out uint portIndex)
        {
            portIndex = default;
            if (m_NodeMapping.TryGetValue(nodeMode, out var mapping))
            {
                portIndex = mapping.FirstPortIndex + m_PortOffsetMappings[nodeMode][portUniqueId];
                return true;
            }
            return false;
        }
    }

    public void BindVariableToInput(VariableHandle variableHandle, in InputDataPort inputPort)
    {
        var portInfo = m_Result.GraphDefinition.PortInfoTable[(int)inputPort.Port.Index];
        portInfo.DataIndex = variableHandle.DataIndex;
        m_Result.GraphDefinition.PortInfoTable[(int)inputPort.Port.Index] = portInfo;
    }

    void CreateEdge(uint outputPortIndex, uint inputPortIndex)
    {
        m_EdgeTable.Add((outputPortIndex, inputPortIndex));

        // Count the number of output edge for each output trigger port
        PortInfo outputPortInfo = m_Result.GraphDefinition.PortInfoTable[(int)outputPortIndex];
        if (!outputPortInfo.IsDataPort)
        {
            outputPortInfo.DataIndex++;
            m_Result.GraphDefinition.PortInfoTable[(int)outputPortIndex] = outputPortInfo;
        }

        PortInfo inputPortInfo = m_Result.GraphDefinition.PortInfoTable[(int)inputPortIndex];
        Assert.AreEqual(outputPortInfo.IsDataPort, inputPortInfo.IsDataPort, "Only ports of the same kind (trigger or data) can be connected");
    }

    public DotsTranslator.DotsCompilationResult Build(DotsStencil.DotsStencil stencil)
    {
        // Compute the output trigger edge table. Start at 1, because 0 is reserved for null
        // For each output trigger port, add an entry if they have at least 1 edge. Allocate an extra slot for the trailing null
        uint edgeTableSize = 1;
        var definition = m_Result.GraphDefinition;

        for (int i = 0; i < definition.PortInfoTable.Count; i++)
        {
            var port = definition.PortInfoTable[i];
            if (port.IsOutputPort && !port.IsDataPort)
            {
                uint nbOutputEdge = port.DataIndex + 1;
                if (nbOutputEdge > 1)
                {
                    port.DataIndex = edgeTableSize;
                    definition.PortInfoTable[i] = port;
                    edgeTableSize += nbOutputEdge;
                }
            }
        }

        // Fill the table
        for (uint i = 0; i < edgeTableSize; i++)
            definition.TriggerTable.Add(0);

        var connectedOutputDataPorts = new HashSet<PortInfo>();
        // Process the edge table
        foreach (var edge in m_EdgeTable)
        {
            // Retrieve the input & output port info
            PortInfo outputPortInfo = definition.PortInfoTable[(int)edge.Item1];
            PortInfo inputPortInfo = definition.PortInfoTable[(int)edge.Item2];
            Assert.AreEqual(outputPortInfo.IsDataPort, inputPortInfo.IsDataPort, "Only ports of the same kind (trigger or data) can be connected");
            if (outputPortInfo.IsDataPort)
            {
                // For data port, copy the DataIndex of the output port in the dataindex of the input port &
                // Keep track of the output node (because we will pull on it & execute it)
                // TODO: Optim opportunity here: We could detect flownode & constant here & avoid runtime checks by cutting link
                inputPortInfo.DataIndex = outputPortInfo.DataIndex;
                definition.DataPortTable[(int)inputPortInfo.DataIndex] = outputPortInfo.NodeId;
                definition.PortInfoTable[(int)edge.Item2] = inputPortInfo;
                connectedOutputDataPorts.Add(outputPortInfo);
            }
            else
            {
                // For trigger port, we need to find a spot in the trigger table & set it
                int triggerTableIndex = (int)outputPortInfo.DataIndex;
                while (definition.TriggerTable[triggerTableIndex] != 0)
                    triggerTableIndex++;
                definition.TriggerTable[triggerTableIndex] = edge.Item2;
            }
        }

        // Reset DataIndex of non-connected outputDataPort
        for (var i = 0; i < definition.PortInfoTable.Count; ++i)
        {
            if (definition.PortInfoTable[i].IsOutputPort && definition.PortInfoTable[i].IsDataPort)
            {
                var isConnected = connectedOutputDataPorts.Contains(definition.PortInfoTable[i]);
                var isVariablePort = m_VariableToDataIndex.Any(h => h.Value.DataIndex == definition.PortInfoTable[i].DataIndex);

                if (!isConnected && !isVariablePort)
                {
                    var port = definition.PortInfoTable[i];
                    port.DataIndex = 0;
                    definition.PortInfoTable[i] = port;
                }
            }
        }

        // Serialize runtime data for each referenced component type
        foreach (var typeReference in m_ReferencedComponentTypeIndices)
        {
            var t = TypeManager.GetType(typeReference.TypeIndex);
            var fields = t.GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType.GenerateTypeHandle(stencil).TryTypeHandleToValueType(out var valueType))
                {
                    var fieldDescription = new ComponentFieldDescription
                    {
                        Name = field.Name,
                        Offset = UnsafeUtility.GetFieldOffset(field),
                        Type = valueType,
                        ComponentTypeHash = typeReference.TypeHash
                    };
                    m_Result.GraphDefinition.ComponentFields.Add(fieldDescription);
                }
                else
                {
                    // skip any field we don't know how to handle
                    Debug.LogWarning($"Skipping {t.FullName}.{field.Name} of type {field.FieldType.Name}");
                }
            }
        }

        if (s_IsGraphDumpEnabled)
            Debug.Log(m_Result.GraphDefinition.GraphDump());

        return m_Result;
    }

    public void CreateDebugSymbols(DotsStencil.DotsStencil stencil)
    {
        ((DotsDebugger)stencil.Debugger).CreateDebugSymbols(m_NodeMapping, m_PortOffsetMappings);
    }

    public static VariableType GetVariableType(IVariableDeclarationModel graphVariableModel)
    {
        VariableDeclarationModel decl = (VariableDeclarationModel)graphVariableModel;
        if (decl.IsSmartObject())
            return VariableType.SmartObject;
        if (decl.IsObjectReference())
            return GraphBuilder.VariableType.ObjectReference;
        if (decl.IsInputOrOutput())
            return VariableType.InputOutput;
        return GraphBuilder.VariableType.Variable;
    }

    public GraphDataInput DeclareInputData(string name, ValueType type)
    {
        var triggerList = m_Result.GraphDefinition.InputDatas;
        var triggerIndex = triggerList.FindIndex(t => t.Name == name);
        Assert.AreEqual(-1, triggerIndex, $"An input data with the same name '{name}' already exists");

        triggerList.Add(new GraphDefinition.InputData(name, type));
        // after adding to the list - first one will have id 1
        var input = new GraphDataInput { InputDataId = (uint)triggerList.Count};
        return input;
    }

    /// <summary>
    /// Declares a graph data output, returns its index + 1 in the data output list
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="dataIndex"></param>
    /// <returns>The data output id (its index + 1 in the data output list)</returns>
    public uint DeclareOutputData(BindingId name, ValueType type, VariableHandle dataIndex)
    {
        Assert.IsTrue(m_VariableToDataIndex.ContainsKey(name));
        var triggerList = m_Result.GraphDefinition.OutputDatas;
        var triggerIndex = triggerList.FindIndex(t => t.Name.Equals(name));
        Assert.AreEqual(-1, triggerIndex, $"An input data with the same name '{name}' already exists");

        // data index will be patched when building the graph
        triggerList.Add(new GraphDefinition.OutputData(dataIndex.DataIndex, name, type));
        return (uint)triggerList.Count;
    }

    /// <summary>
    /// Declares an input trigger, but doesn't add the node to the internal node table
    /// </summary>
    /// <param name="name"></param>
    /// <returns>The graph trigger input node</returns>
    public GraphTriggerInput DeclareInputTrigger(string name)
    {
        var triggerList = m_Result.GraphDefinition.InputTriggers;
        var triggerIndex = triggerList.FindIndex(t => t.Name == name);
        Assert.AreEqual(-1, triggerIndex, $"An input trigger with the same name '{name}' already exists");

        var id = GetNextNodeId();
        var input = new GraphTriggerInput();
        triggerList.Add(new GraphDefinition.InputOutputTrigger(id, name));
        return input;
    }

    public bool GetExistingInputTrigger(string name, out GraphTriggerInput trigger)
    {
        var triggerList = m_Result.GraphDefinition.InputTriggers;
        var triggerIndex = triggerList.FindIndex(t => t.Name == name);
        if (triggerIndex == -1)
        {
            trigger = default;
            return false;
        }
        trigger = (GraphTriggerInput)m_Result.GraphDefinition.NodeTable[(int)triggerList[triggerIndex].NodeId.GetIndex()];
        return true;
    }

    /// <summary>
    /// Declares an output trigger, but doesn't add the node to the internal node table
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GraphTriggerOutput DeclareOutputTrigger(string name)
    {
        List<GraphDefinition.InputOutputTrigger> triggerList = m_Result.GraphDefinition.OutputTriggers;
        int triggerIndex = triggerList.FindIndex(t => t.Name == name);
        Assert.AreEqual(-1, triggerIndex, $"An output trigger with the same name '{name}' already exists");

        NodeId id = GetNextNodeId();
        int outputIndex = triggerList.Count;
        GraphTriggerOutput input = new GraphTriggerOutput { OutputIndex = (uint)outputIndex};
        triggerList.Add(new GraphDefinition.InputOutputTrigger(id, name));
        return input;
    }

    public bool GetExistingOutputTrigger(string name, out GraphTriggerOutput trigger)
    {
        var triggerList = m_Result.GraphDefinition.OutputTriggers;
        var triggerIndex = triggerList.FindIndex(t => t.Name == name);
        if (triggerIndex == -1)
        {
            trigger = default;
            return false;
        }
        trigger = (GraphTriggerOutput)m_Result.GraphDefinition.NodeTable[(int)triggerList[triggerIndex].NodeId.GetIndex()];
        return true;
    }

    public void AddReferencedComponent(TypeReference typeReference)
    {
        m_ReferencedComponentTypeIndices.Add(typeReference);
    }

    [MenuItem("internal:" + k_GraphDumpMenu, false)]
    static void CreateGraphDumpMenu(MenuCommand menuCommand)
    {
        s_IsGraphDumpEnabled = !s_IsGraphDumpEnabled;
    }

    [MenuItem("internal:" + k_GraphDumpMenu, true)]
    static bool SwitchGraphDumpMenu()
    {
        Menu.SetChecked(k_GraphDumpMenu, s_IsGraphDumpEnabled);
        return true;
    }

    public void BindSubgraph(uint subgraphEntityDataIndex, VSGraphAssetModel subgraph)
    {
        if (!subgraph || !(subgraph.GraphModel?.Stencil is DotsStencil.DotsStencil subgraphStencil))
        {
            Unity.Assertions.Assert.IsTrue(false);
            return;
        }
        if (!subgraphStencil.CompiledScriptingGraphAsset)
            DotsGraphTemplate.CreateDotsCompiledScriptingGraphAsset(subgraph.GraphModel);
        BindSubgraph(subgraphEntityDataIndex, subgraphStencil.CompiledScriptingGraphAsset);
    }

    internal void BindSubgraph(uint subgraphEntityDataIndex, ScriptingGraphAsset subgraphAsset)
    {
        m_Result.GraphDefinition.SubgraphReferences.Add(
            new GraphDefinition.SubgraphReference(subgraphEntityDataIndex, subgraphAsset));
    }
}
