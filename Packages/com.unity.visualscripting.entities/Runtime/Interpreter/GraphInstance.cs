#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define VS_TRACING
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;
#if VS_DOTS_PHYSICS_EXISTS
using VisualScripting.Physics;
#endif

namespace Runtime
{
    /// <summary>
    /// The state of an instance running a graph (active nodes, port values, ...)
    /// </summary>
    public class GraphInstance : IGraphInstance, IDisposable
    {
        const int k_MaxNodesPerFrame = 1024;
        readonly GraphDefinition m_Definition;

        // TODO use a union of all possible value types ?
        Dictionary<uint, Value> m_DataValues;

        /// Each node instance's state
        /// TODO nodestate replace that by a malloced pointer, assign an offset and store in nodes during baking, use it to index this allocated region
        NativeArray<byte> m_NodeStates;
        int[] m_NodeStateOffsets;
        /// Stores the state of the IFlowNode&lt;TState&gt; being executed
        unsafe void* m_CurrentNodeState;
        bool m_IsNodeCurrentlyScheduledForUpdate;

        /////////////////////////////////////////////////////////////
        /// Logging system
        [Flags]
        public enum LogItem
        {
            None = 0,               // Log nothing
            Init = 1,               // Initialization sequence (const, etc.)
            Flow = 2,               // Node flow; what execute when
            PortRead = 4,           // Data port Read Access
            PortWrite = 8,          // Data port Write Access
            Node = 16,              // Node internal operation
            Graph = 32,             // Display whole graph data at startup
            Performance = 64,       // Performance measurments
            EntryPoint = 128,       // Log entry point flows
            Full = 65535
        }

        private List<string> logArray = new List<string>();

        // Log Filter
        private LogItem logFilter = LogItem.None;

        public void Log(string message, LogItem item = LogItem.Node)
        {
            if ((logFilter & item) != 0)
                logArray.Add(message);
        }

        private string PortToString(int index)
        {
            return "Port(" + index + ", " + (index < m_Definition.PortInfoTable.Count ? m_Definition.PortInfoTable[index].PortName : "Unknown") + ")";
        }

        private void DumpLog()
        {
            if (logArray.Count > 0)
                Debug.Log(string.Join("\n\r", logArray.ToArray()));
        }

        private void ClearLog()
        {
            logArray.Clear();
        }

        public unsafe ref T GetState<T>(in IFlowNode<T> _) where T : unmanaged, INodeState
        {
            Assert.IsFalse(m_CurrentNodeState == null);
            Assert.IsFalse(UnsafeUtility.SizeOf<T>() == 0);
            return ref UnsafeUtilityEx.AsRef<T>(m_CurrentNodeState);
        }

#if VS_TRACING
        public DotsFrameTrace FrameTrace { get; set; }
        // Used for flushing the FrameTrace to the right graph without having to make a query and makes it easier to flush destroyed graphs
        public int ScriptingGraphAssetID { get; set; }
#endif
        /// <summary>
        /// Node execution entry point
        /// </summary>
        public struct NodeExecution
        {
            /// <summary>
            /// triggered node index
            /// </summary>
            public NodeId   nodeId;
            /// <summary>
            /// triggered port index
            /// </summary>
            public uint portIndex;

            public ActiveNodesState SavedState;
        }

        ActiveNodesState _state;

        public TimeData Time { get; set; }
        public uint LastSystemVersion { get; set; }
        private bool m_IsStarting = true;
        public bool IsStarting
        {
            get { return m_IsStarting; }
            set { m_IsStarting = value; }
        }

        /// <summary>
        /// Filled when the current graph execution must be interrupted, a separate graph must be executed then the first graph must be resumed
        /// </summary>
        // public InterruptContext InterruptContext;

        Random m_Random;

        public Random Random
        {
            get
            {
                var copy = m_Random;
                m_Random.NextUInt(); // change internal state so that we give different values every time this frame
                return copy;
            }
        }

        public Entity CurrentEntity { get; private set; }
        private static Dictionary<Type, Type> NodeTypeToNodeStateType = new Dictionary<Type, Type>();
        public EntityManager EntityManager { get; private set; }

        List<DotsEventData> m_DispatchedEvents;
        public IEnumerable<DotsEventData> DispatchedEvents => m_DispatchedEvents;

        static Type GetNodeStateType(IBaseFlowNode n)
        {
            if (n is IFlowNode || n is IEventNode)
                return null;
            var nodeType = n.GetType();
            if (!NodeTypeToNodeStateType.TryGetValue(nodeType, out var t))
            {
                var iFlowNodeInterface = nodeType.GetInterfaces().SingleOrDefault(i =>
                    i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IFlowNode<>));
                Assert.IsNotNull(iFlowNodeInterface);
                t = iFlowNodeInterface.GetGenericArguments()[0];
                NodeTypeToNodeStateType.Add(nodeType, t);
            }

            return t;
        }

        public static GraphInstance Create(GraphDefinition definition, EntityManager entityManager,
            DynamicBuffer<ValueInput> inputs) => definition?.NodeTable == null || definition.PortInfoTable == null
        ? null
        : new GraphInstance(definition, entityManager, inputs);

        GraphInstance(GraphDefinition definition, EntityManager entityManager, DynamicBuffer<ValueInput> inputs)
        {
            EntityManager = entityManager;
            m_Definition = definition;
            m_DispatchedEvents = new List<DotsEventData>();
            _state.Init();
            m_DataValues = new Dictionary<uint, Value>();

            // Dumping Graph
            if ((logFilter & LogItem.Graph) != 0)
                Debug.Log(m_Definition.GraphDump());

            // TODO bake NodeStateOffsets and totalSize in the definition during translation. to reduce the size of unused slots we could sort nodes in the definition itself (nodes with state first, everything else after)
            m_NodeStateOffsets = new int[definition.NodeTable.Count];
            int totalSize = 0;
            for (var i = 0; i < definition.NodeTable.Count; i++)
            {
                // IFlowNode doesn't have a state, IFlowNode<> does
                if (!(definition.NodeTable[i] is IBaseFlowNode baseFlowNode) || baseFlowNode is IFlowNode)
                {
                    m_NodeStateOffsets[i] = -1;
                    continue;
                }
                int stateSize = GetNodeStateSize(baseFlowNode);
                Assert.AreNotEqual(0, stateSize); //
                m_NodeStateOffsets[i] = totalSize;
                totalSize += stateSize;
            }
            m_NodeStates = new NativeArray<byte>(totalSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // Update constants
            for (var i = 0; i < definition.NodeTable.Count; i++)
            {
                // TODO: How can we only select IConstantNode<T>
                if (definition.NodeTable[i] is IConstantNode constantNode)
                {
                    Log("Executing Constant node " + i + " " + constantNode.GetType().ToString(), LogItem.Init);
                    IDataNode dataNode = constantNode;
                    ExecuteDataNode(ref dataNode);
                    constantNode = (IConstantNode)dataNode;
                }
            }

            foreach (var initValue in definition.VariableInitValues)
            {
                m_DataValues[initValue.DataIndex] = initValue.Value;
            }

            if (inputs.IsCreated)
            {
                foreach (var valueInput in inputs)
                {
                    m_DataValues[valueInput.Index] = valueInput.Value;
                }
            }

            // WARNING: inputs must be iterated over BEFORE this, as any entity manager operation will invalidate the dynamic buffer handle
            foreach (var subgraphReference in definition.SubgraphReferences)
            {
                var subgraphEntity = entityManager.CreateEntity(typeof(ScriptingGraphInstance), typeof(ScriptingGraphInstanceAlive));
                m_DataValues[subgraphReference.SubgraphEntityDataIndex] = subgraphEntity;
                entityManager.AddSharedComponentData(subgraphEntity,
                    new ScriptingGraph {ScriptingGraphAsset = subgraphReference.Subgraph});
            }
        }

        static int GetNodeStateSize(IBaseFlowNode baseFlowNode)
        {
            var stateType = GetNodeStateType(baseFlowNode);
            return stateType == null ? 0 : UnsafeUtility.SizeOf(stateType);
        }

        static void AssertPortIsValid(Port p)
        {
            Assert.AreNotEqual(0, p.Index, "Port has not been initialized: its index is 0");
        }

        public void Write(OutputDataPort port, Value value)
        {
            AssertPortIsValid(port.GetPort());
            Assert.IsTrue(port.GetPort().Index < m_Definition.PortInfoTable.Count);
            int portIndex = (int)port.GetPort().Index;
            uint dataSlot = m_Definition.PortInfoTable[portIndex].DataIndex;
#if VS_TRACING
            FrameTrace?.RecordWrittenValue(value, port);
#endif
            m_DataValues.TryGetValue(dataSlot, out var oldValue);
            Log(PortToString(portIndex) + " writing in slot " + dataSlot + " : " + oldValue + " => " + value, LogItem.PortWrite);

            WriteValueToDataSlot(dataSlot, value);
        }

        public Value ReadGraphOutputValue(int graphOutputIndex)
        {
            var dataPortIndex = m_Definition.OutputDatas[graphOutputIndex].DataPortIndex;
            if (dataPortIndex == 0)
                return default;

            return m_DataValues[dataPortIndex];
        }

        public Value ReadGraphInputValue(int graphInputIndex) => ScriptingGraphRuntime.ReadGraphInputValue(graphInputIndex);

        internal void WriteValueToDataSlot(uint dataSlot, Value value)
        {
            m_DataValues[dataSlot] = value;
        }

        internal Value ReadDataSlot(uint dataSlot)
        {
            return m_DataValues[dataSlot];
        }

        public bool ReadBool(InputDataPort port) => ReadValue(port, out Value val) ? val.Bool : default;
        public int ReadInt(InputDataPort port) => ReadValue(port, out Value val) ? val.Int : default;
        public float ReadFloat(InputDataPort port) => ReadValue(port, out Value val) ? val.Float : default;
        public float2 ReadFloat2(InputDataPort port) => ReadValue(port, out Value val) ? val.Float2 : default;
        public float3 ReadFloat3(InputDataPort port) => ReadValue(port, out Value val) ? val.Float3 : default;
        public float4 ReadFloat4(InputDataPort port) => ReadValue(port, out Value val) ? val.Float4 : default;
        public quaternion ReadQuaternion(InputDataPort port) => ReadValue(port, out Value val) ? val.Quaternion : default;
        public Entity ReadEntity(InputDataPort port) => ReadValue(port, out Value val) ? val.Entity : default;
        public Value ReadValue(InputDataPort port) => ReadValue(port, out Value val) ? val : default;
        public Value ReadValueOfType(InputDataPort port, ValueType valueType) => ReadValue(port, out Value val, valueType) ? val : default;

        static MethodInfo s_GetComponentDataRawRwMi;
        public ScriptingGraphRuntime ScriptingGraphRuntime;
        ActiveNodesState m_CurrentExecutionSavedState;

        unsafe void* GetComponentDataRawRW(Entity entity, int typeIndex)
        {
            if (s_GetComponentDataRawRwMi == null)
            {
                s_GetComponentDataRawRwMi = typeof(EntityManager).GetMethod("GetComponentDataRawRW", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            Assert.IsNotNull(s_GetComponentDataRawRwMi);
            return Pointer.Unbox(s_GetComponentDataRawRwMi.Invoke(EntityManager, new object[] { entity, typeIndex }));
        }

        bool GetComponentFieldDescription(TypeReference componentType, int fieldIndex, out ComponentFieldDescription description)
        {
            description = default;
            if (m_Definition.FieldDescriptions.TryGetValue(componentType.TypeHash, out var fieldDescriptions))
            {
                if (fieldIndex >= 0 && fieldIndex < fieldDescriptions.Count)
                {
                    description = fieldDescriptions[fieldIndex];
                    return true;
                }
            }
            return false;
        }

        unsafe void* GetComponentData(Entity e, int typeIndex, int offset)
        {
            void* componentPtr = GetComponentDataRawRW(e, typeIndex);
            return (byte*)componentPtr + offset;
        }

        public Value GetComponentDefaultValue(TypeReference componentType, int fieldIndex)
        {
            if (GetComponentFieldDescription(componentType, fieldIndex, out var desc))
            {
                return new Value { Type = desc.Type };
            }

            return default;
        }

        public unsafe Value GetComponentValue(Entity e, TypeReference componentType, int fieldIndex)
        {
            if (GetComponentFieldDescription(componentType, fieldIndex, out var desc))
            {
                var dataPtr = GetComponentData(e, componentType.TypeIndex, desc.Offset);
                return Value.FromPtr(dataPtr, desc.Type);
            }

            return default;
        }

        public unsafe void SetComponentValue(Entity e, TypeReference componentType, int fieldIndex, Value value)
        {
            if (GetComponentFieldDescription(componentType, fieldIndex, out var desc))
            {
                var dataPtr = GetComponentData(e, componentType.TypeIndex, desc.Offset);
                Value.SetPtrToValue(dataPtr, desc.Type, value);
            }
        }

        private bool ReadValue(InputDataPort port, out Value value, ValueType coerceToType = ValueType.Unknown)
        {
            // An intput data node has at most one incoming edge
            // The output & input data port linked by an edge will point to the same Data index
            AssertPortIsValid(port.GetPort());
            Assert.IsTrue(port.GetPort().Index < m_Definition.PortInfoTable.Count);

            int portIndex = (int)port.GetPort().Index;
            var portInfo = m_Definition.PortInfoTable[portIndex];
            Assert.IsTrue(portInfo.IsDataPort, $"Trying to read a value from a trigger port: {portInfo.PortName}:{port.Port.Index} in node {portInfo.NodeId}:{m_Definition.NodeTable[(int) portInfo.NodeId.GetIndex()]}");
            uint dataSlot = portInfo.DataIndex;

            if (dataSlot != 0)
            {
                NodeId dependencyNodeId = m_Definition.DataPortTable[(int)dataSlot];

                // Only pull data nodes
                // Disconnected ports have 0 (NULL) as daat Slots
                // Do not pull on constant nodes, since they never change
                if (dependencyNodeId.IsValid() && // if the port is connected to a variable, it will have a data slot but no dependent node, as the "get variable" does not generate a runtime node
                    m_Definition.NodeTable[(int)dependencyNodeId.GetIndex()] is IDataNode dataNode
                    && !(dataNode is IConstantNode))
                {
                    Log("Pulling Data Node " + dependencyNodeId.GetIndex() + " (" + dataNode.GetType() + ")", LogItem.Flow);
#if VS_TRACING
                    FrameTrace?.RecordExecutedNode(dependencyNodeId, 0);
#endif
                    ExecuteDataNode(ref dataNode);
                }

                var tryGetValue = m_DataValues.TryGetValue(dataSlot, out var val);
                Log(PortToString(portIndex) + " read slot " + dataSlot + " <= " + val, LogItem.PortRead);

                if (tryGetValue)
                {
                    value = CoerceValueToType(coerceToType, val);
#if VS_TRACING
                    FrameTrace?.RecordReadValue(value, port);
#endif
                    return true;
                }
            }

            value = default;
#if VS_TRACING
            FrameTrace?.RecordReadValue(value, port);
#endif
            return false;
        }

        static Value CoerceValueToType(ValueType coerceToType, Value val)
        {
            switch (coerceToType)
            {
                case ValueType.Unknown:
                    return val;
                case ValueType.Bool:
                    return val.Bool;
                case ValueType.Int:
                    return val.Int;
                case ValueType.Float:
                    return val.Float;
                case ValueType.Float2:
                    return val.Float2;
                case ValueType.Float3:
                    return val.Float3;
                case ValueType.Float4:
                    return val.Float4;
                case ValueType.Quaternion:
                    return val.Quaternion;
                case ValueType.Entity:
                    return val.Entity;
                case ValueType.StringReference:
                    return val.StringReference;
                default:
                    throw new ArgumentOutOfRangeException(nameof(coerceToType), coerceToType, null);
            }
        }

        private void ExecuteDataNode(ref IDataNode dataNode)
        {
            dataNode.Execute(this);
        }

        public Execution RunNestedGraph(in GraphReference graphReference, Entity target, int triggerIndex)
        {
            var currentExecutionSavedState = m_CurrentExecutionSavedState;
            var runNestedGraph = ScriptingGraphRuntime.RunNestedGraph(this, ref currentExecutionSavedState, graphReference, target, triggerIndex);
            m_CurrentExecutionSavedState = currentExecutionSavedState;
            return runNestedGraph;
        }

        public Execution TriggerGraphOutput(uint outputIndex)
        {
            return ScriptingGraphRuntime.TriggerGraphOutput(this, outputIndex);
        }

        public void TriggerGraphInput(string output)
        {
            var it = this.m_Definition.InputTriggers.FirstOrDefault(i => i.Name == output);
            Assert.IsTrue(it.NodeId.IsValid());
            _state.AddExecutionThisFrame(it.NodeId);
        }

        public void Trigger(OutputTriggerPort output)
        {
#if VS_TRACING
            FrameTrace?.RecordTriggeredPort(output);
#endif

            int portIndex = (int)output.Port.Index;
            Assert.IsTrue(portIndex < m_Definition.PortInfoTable.Count);
            int triggerIndex = (int)m_Definition.PortInfoTable[portIndex].DataIndex;
            Assert.IsTrue(triggerIndex < m_Definition.TriggerTable.Count);

            // find null terminating item in trigger table, then go backward, as  AddExecutionThisFrame is pushing items on a stack
            // TODO reverse trigger table in the first place ?
            int i;
            for (i = triggerIndex; m_Definition.TriggerTable[i] != 0; i++) {}

            for (i--; i >= triggerIndex; i--)
            {
                uint portToExecute = m_Definition.TriggerTable[i];
                Assert.IsTrue(portToExecute < m_Definition.PortInfoTable.Count);
                _state.AddExecutionThisFrame(m_Definition.PortInfoTable[(int)portToExecute].NodeId, portToExecute);
            }
        }

        /// <summary>
        /// This function is meant to be called from inside a node Execute() member
        /// This variable is set in Execute().
        /// Not the cleanest code, but it avoid passing an extra parameter to Execute()
        /// </summary>
        /// <returns>It returns true if the node is scheduled for the next update</returns>
        public bool IsNodeCurrentlyScheduledForUpdate()
        {
            return m_IsNodeCurrentlyScheduledForUpdate;
        }

        /// <summary>
        /// Run a node, either from a trigger port, or an EntryPoint
        /// </summary>
        /// <param name="exec">Description of the triggered node and port</param>
        /// <param name="alreadyRunning"></param>
        /// <param name="evt">The event that is being processed</param>
        /// <returns>status of execution, whether the task is done or ongoing</returns>
        /// <exception cref="InvalidDataException">Thrown if unable to run the node</exception>
        private unsafe Execution ExecuteNode(NodeExecution exec, bool alreadyRunning, DotsEventData evt = default)
        {
            Assert.IsTrue(exec.nodeId.IsValid());
            NodeId nodeId = exec.nodeId;
            var node = m_Definition.NodeTable[(int)nodeId.GetIndex()];

            Log("Executing Node " + nodeId.GetIndex() + " (" + node.GetType() + ")", node is IEntryPointNode ? LogItem.EntryPoint : LogItem.Flow);

            var execution = ExecuteNode(exec, alreadyRunning, evt, node, nodeId);

#if VS_TRACING
            byte progress = Byte.MinValue;
            if (node is INodeReportProgress reportProgress)
            {
                progress = reportProgress.GetProgress(this);
                if (execution == Execution.Done)
                    progress = byte.MaxValue;
            }
            FrameTrace?.RecordExecutedNode(exec.nodeId, progress);
#endif
            // Do that AFTER the recording, which calls GetProgress, which relies on the state
            m_CurrentNodeState = null;
            return execution;
        }

        private unsafe Execution ExecuteNode(NodeExecution exec, bool alreadyRunning, DotsEventData evt, INode node, NodeId nodeId)
        {
            // Keep track if this node is planned for execution in Update
            m_IsNodeCurrentlyScheduledForUpdate = alreadyRunning;

            if (node is IEntryPointNode entryPointNode)
            {
                entryPointNode.Execute(this);
                return Execution.Done;
            }
            var inputPort = new InputTriggerPort {Port = new Port { Index = exec.portIndex }};
            switch (node)
            {
                case IEventReceiverNode eventReceiverNode:
                    if (evt.TargetEntity == CurrentEntity || evt.TargetEntity == default)
                        return eventReceiverNode.Execute(this, evt);
                    return Execution.Done;
                case IFlowNode flowNode:
                    flowNode.Execute(this, inputPort);
                    return Execution.Done;
                case IDataNode _:
                    Assert.IsTrue(false);
                    break;
                case IStateFlowNode stateFlowNode:
                {
                    var nodeStateOffset = m_NodeStateOffsets[nodeId.GetIndex()];
                    Assert.IsTrue(nodeStateOffset >= 0);
                    m_CurrentNodeState = (byte*)m_NodeStates.GetUnsafePtr() + nodeStateOffset;

                    // TODO theor this is terrible. See NodeStates' comment for proper solution
                    // We know all INodeState implementors are structs, but using the interface in the dictionary forces
                    // the compiler to box them. Pin the boxed object, store its pointer in CurrentNodeState, then copy it
                    // back into the dictionary;
                    Execution execution;
                    if (exec.portIndex == 0)
                        execution = stateFlowNode.Update(this);
                    else
                        execution = stateFlowNode.Execute(this, inputPort);
                    return execution;
                }
            }

            throw new InvalidDataException();
        }

        /// <summary>
        /// Prepare for the graph execution this frame
        /// </summary>
        public void ResetFrame()
        {
            m_Random = new Random((uint)(CurrentEntity.Index + 1) * (LastSystemVersion + 1) * 0x9F6ABC1);
            _state.NodesToExecute.Clear();
            m_DispatchedEvents.Clear();
        }

        // public unsafe bool RunFrameNested<TGraphInstance>(Entity e, TimeData time, InterruptContext interruptContext, TGraphInstance parentContext) where TGraphInstance : IGraphInstance
        // {
        // throw new NotImplementedException();
        // m_ParentContextData = new ParentContextData(interruptContext, parentContext);
        // return RunFrame(e, time);
        // }

        /// <summary>
        /// Runs nodes in the <see cref="ActiveNodesState.NodesToExecute"/> list and add them to the <see cref="ActiveNodesState.NextFrameNodes"/> list if needed
        /// </summary>
        /// <param name="e"></param>
        /// <param name="time"></param>
        /// <param name="evt"></param>
        public bool ResumeFrame(Entity e, TimeData time, DotsEventData evt)
        {
            Time = time;
            var startTime = UnityEngine.Time.realtimeSinceStartup;
            ClearLog();
            Log("GraphInstance executing event" + evt, LogItem.EntryPoint);

            CurrentEntity = e;

            int nodeExecuted = 0;
            bool interrupt = false;
            while (_state.NodesToExecute.Count > 0 && !interrupt)
            {
                // Check for endless cycle & stop when k_MaxNodesPerFrame are exectued
                if (nodeExecuted++ >= k_MaxNodesPerFrame)
                {
                    Debug.LogWarning($"Trying to execute more than {k_MaxNodesPerFrame} nodes in a frame, something seems wrong.");
                    break;
                }

                // Pop top node & Execute it
                var activeExec = _state.NodesToExecute.Pop();
                m_CurrentExecutionSavedState = activeExec.SavedState;
                bool alreadyRunning = _state.NextFrameNodes.Any(ex => ex.nodeId.GetIndex() == activeExec.nodeId.GetIndex());
                var exec = ExecuteNode(activeExec, alreadyRunning, evt);
                switch (exec)
                {
                    case Execution.Interrupt:
                        interrupt = true;
                        break;
                    // If the node needs to execute & wait for a while, we reschedule it for next frame
                    // else, If the current call stop/disable the node, ensure it is NOT executed next frame
                    case Execution.Running:
                    {
                        if (!alreadyRunning)
                            _state.NextFrameNodes.Add(new NodeExecution { nodeId = activeExec.nodeId, portIndex = 0, SavedState = m_CurrentExecutionSavedState});
                        break;
                    }
                }
                m_CurrentExecutionSavedState = default;
            }

            Log($"Entity {GetString(CurrentEntity)} ran {nodeExecuted} nodes this frame", LogItem.Performance);
            var durationTime = UnityEngine.Time.realtimeSinceStartup - startTime;
            Log("Graph execution time for event " + evt + " = " + durationTime * 1000000 + "usec", LogItem.Performance);
            DumpLog();

            CurrentEntity = Entity.Null;
            return _state.NodesToExecute.Count > 0;
        }

        /// <summary>
        /// DFS execution of the graph
        /// </summary>
        /// <param name="e">Entity running the graph</param>
        /// <param name="time"></param>
        /// <param name="evt">The event that is being processed</param>
        public void RunFrame(Entity e, TimeData time, DotsEventData evt = default)
        {
            _state.MoveStateToNextFrame();

            ResumeFrame(e, time, evt);
        }

        /// <summary>
        /// Trigger every entry point of a specific type4
        /// </summary>
        /// <typeparam name="T">Type of entry points to trigger</typeparam>
        public void TriggerEntryPoints<T>() where T : struct, IEntryPointNode
        {
            for (int i = 0; i < m_Definition.NodeTable.Count; i++)
            {
                var index = i;
                var n = m_Definition.NodeTable[i];
                if (n is T)
                {
                    _state.AddExecutionThisFrame(new NodeId((uint)index));
                }
            }
        }

        /// <summary>
        /// Trigger every events
        /// </summary>
        /// <typeparam name="T">Type of event to trigger</typeparam>
        public void TriggerEvents<T>() where T : struct, IEventReceiverNode
        {
            for (int i = 0; i < m_Definition.NodeTable.Count; i++)
            {
                var index = i;
                var n = m_Definition.NodeTable[i];
                if (n is T)
                {
                    _state.AddExecutionThisFrame(new NodeId((uint)index));
                }
            }
        }

        /// <summary>
        /// Record the event to be sent
        /// </summary>
        /// <param name="evt">Data of the event</param>
        public void DispatchEvent(DotsEventData evt)
        {
            m_DispatchedEvents.Add(evt);
        }

        public int GetTriggeredIndex(InputTriggerMultiPort nodePort, InputTriggerPort triggeredPort)
        {
            if ((triggeredPort.Port.Index >= nodePort.Port.Index) && (triggeredPort.Port.Index < nodePort.Port.Index + nodePort.DataCount))
                return (int)(triggeredPort.Port.Index - nodePort.Port.Index);
            return -1;
        }

        public bool HasConnectedValue(IOutputDataPort port)
        {
            return m_Definition.HasConnectedValue(port);
        }

        public bool HasConnectedValue(IOutputTriggerPort port)
        {
            return m_Definition.HasConnectedValue(port);
        }

        public bool HasConnectedValue(IInputDataPort port)
        {
            return m_Definition.HasConnectedValue(port);
        }

        public void Dispose()
        {
#if VS_TRACING
            FrameTrace?.Dispose();
#endif
            m_NodeStates.Dispose();
        }

        public NativeString128 GetString(StringReference messageStringReference)
        {
            return m_Definition.Strings ? [messageStringReference.Index];
        }

        public NativeString128 GetString(Entity entity)
        {
#if UNITY_EDITOR
            return entity == Entity.Null ? entity.ToString() : EntityManager.GetName(entity);
#else
            return entity.ToString();
#endif
        }

        public GraphDefinition.InputOutputTrigger GetInputTrigger(int inputTriggerIndex)
        {
            return m_Definition.InputTriggers[inputTriggerIndex];
        }

        public struct ActiveNodesState
        {
            /// <summary>
            /// List of nodes to consider this frame
            /// </summary>
            public Stack<NodeExecution> NodesToExecute;
            /// <summary>
            /// List of nodes to execute next frame
            /// </summary>
            public List<NodeExecution> NextFrameNodes;
            public bool AnyNodeNextFrame => NextFrameNodes != null && NextFrameNodes.Count > 0;

            public void Init()
            {
                Assert.IsNull(NodesToExecute);
                Assert.IsNull(NextFrameNodes);
                NodesToExecute = new Stack<NodeExecution>(k_MaxNodesPerFrame);
                NextFrameNodes = new List<NodeExecution>();
            }

            /// <summary>
            /// Plan an execution this frame
            /// </summary>
            /// <param name="nodeId">Node to trigger</param>
            /// <param name="portIndex">Trigger port</param>
            public void AddExecutionThisFrame(NodeId nodeId, uint portIndex = 0)
            {
                AddExecutionThisFrame(new NodeExecution {nodeId = nodeId, portIndex = portIndex});
            }

            public void AddExecutionThisFrame(NodeExecution exec, uint portIndex = 0)
            {
                Assert.IsTrue(exec.nodeId.IsValid());
                if (NodesToExecute == null)
                    Init();
                NodesToExecute.Push(exec);
            }

            /// <summary>
            /// Fetch ongoing tasks from last frame
            /// </summary>
            public void MoveStateToNextFrame()
            {
                // if(activeNodesState.m_NodesToExecute != null && activeNodesState.m_NodesToExecute.Count != 0)
                //     Debug.LogError("Trying to move state to next frame with nodes present in the current frame list");
                if (NextFrameNodes == null)
                    return;
                foreach (var nodeId in NextFrameNodes)
                    AddExecutionThisFrame(nodeId);
                NextFrameNodes.Clear();
            }
        }

        public ActiveNodesState SaveActiveNodesState() => _state;

        public void RestoreActiveNodesState(ActiveNodesState savedState) => _state = savedState;
    }

    // public struct InterruptContext
    // {
    //     /// <summary>
    //     /// The entity holding the graph to execute
    //     /// </summary>
    //     public Entity NestedGraphEntity;
    //
    //     /// <summary>
    //     /// The activated input trigger node index in the matching list
    //     /// </summary>
    //     public int inputTriggerIndex;
    //
    //     /// <summary>
    //     /// The pointer to the state of the graphReferenceNode, which holds an index of the last activated output trigger
    //     /// </summary>
    //     public unsafe GraphReference.State* StatePointer;
    //
    //     /// <summary>
    //     /// The node causing the interruption and that will be updated when resuming its graph execution
    //     /// </summary>
    //     public NodeId InterruptingNodeId;
    //
    //     /// <summary>
    //     /// The input data port of the graph reference node. When running a nested graph, the graph's data inputs read calls will be forwarded to that port
    //     /// </summary>
    //     public InputDataMultiPort InputDataPort;
    //
    //     /// <summary>
    //     /// The output data port of the graph reference node. After running a nested graphs, the nested graph output values will be copied to that port
    //     /// </summary>
    //     public OutputDataMultiPort OutputDataPort;
    // }
}
