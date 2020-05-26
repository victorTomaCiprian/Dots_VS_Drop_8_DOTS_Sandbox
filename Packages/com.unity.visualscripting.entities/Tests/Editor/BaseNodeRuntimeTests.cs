using System;
using DotsStencil;
using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using ValueType = Runtime.ValueType;

namespace UnityEditor.VisualScriptingECSTests
{
    public abstract class BaseNodeRuntimeTests<T> where T : struct, INode
    {
        protected T m_Node;
        protected Mock<TestGraphInstance> m_GraphInstanceMock;
        protected World m_World;

        // ReSharper disable once MemberCanBeProtected.Global

        [SetUp]
        public virtual void SetUp()
        {
            m_GraphInstanceMock = new Mock<TestGraphInstance>(MockBehavior.Default);
            m_World = new World(GetType().ToString());
        }

        [TearDown]
        public void TearDown()
        {
            m_World.Dispose();
            m_GraphInstanceMock?.Object?.Dispose();
        }

        protected void CustomSetup()
        {
            m_Node = SetupNode(m_Node);
        }

        protected T SetupNode(T node)
        {
            TranslationSetupContext setup = new TranslationSetupContext();
            INode boxed = node;
            foreach (var fieldInfo in BaseDotsNodeModel.GetNodePorts(node.GetType()))
            {
                setup.SetupPort(boxed, fieldInfo, out _, out _, out _);
            }

            return (T)boxed;
        }

        protected void AssertPortTriggered(OutputTriggerPort nOutput, Times times)
        {
            m_GraphInstanceMock.Verify(x => x.Trigger(nOutput), times);
        }
    }

    public abstract class TestGraphInstance : IGraphInstance, IDisposable
    {
        unsafe void* m_State;
        public TimeData Time { get; set; }

        public unsafe void SetupState<TS>(in IFlowNode<TS> _) where TS : unmanaged, INodeState
        {
            m_State = UnsafeUtility.Malloc(sizeof(TS), UnsafeUtility.AlignOf<TS>(), Allocator.Temp);
            UnsafeUtility.MemClear(m_State, sizeof(TS));
        }

        public unsafe void Dispose()
        {
            if (m_State != null)
                UnsafeUtility.Free(m_State, Allocator.Temp);
        }

        public unsafe ref T GetState<T>(in IFlowNode<T> _) where T : unmanaged, INodeState
        {
            Assert.IsFalse(m_State == null, "Node State was null. Call SetupState first");
            return ref UnsafeUtilityEx.AsRef<T>(m_State);
        }

        public bool IsNodeCurrentlyScheduledForUpdate()
        {
            return false;
        }

        public abstract Random Random { get; }
        public abstract Entity CurrentEntity { get; }
        public abstract EntityManager EntityManager { get; }

        public abstract NativeString128 GetString(StringReference messageStringReference);
        public abstract NativeString128 GetString(Entity entity);
        public abstract void DispatchEvent(DotsEventData evt);
        public abstract void Log(string message, GraphInstance.LogItem item = GraphInstance.LogItem.Node);

        public int GetTriggeredIndex(InputTriggerMultiPort nodePort, InputTriggerPort triggeredPort)
        {
            if ((triggeredPort.Port.Index >= nodePort.Port.Index) && (triggeredPort.Port.Index < nodePort.Port.Index + nodePort.DataCount))
                return (int)(triggeredPort.Port.Index - nodePort.Port.Index);
            return -1;
        }

        public abstract void Trigger(OutputTriggerPort output);
        public Execution RunNestedGraph(in GraphReference _, Entity target, int triggerIndex)
        {
            throw new NotImplementedException();
        }

        public Execution TriggerGraphOutput(uint outputIndex)
        {
            throw new NotImplementedException();
        }

        public abstract void Write(OutputDataPort port, Value value);
        public Value ReadGraphOutputValue(int graphOutputIndex)
        {
            throw new NotImplementedException();
        }

        public abstract Value ReadGraphInputValue(int graphInputIndex);

        public abstract bool ReadBool(InputDataPort port);
        public abstract int ReadInt(InputDataPort port);
        public abstract float ReadFloat(InputDataPort port);
        public abstract float2 ReadFloat2(InputDataPort port);
        public abstract float3 ReadFloat3(InputDataPort port);
        public abstract float4 ReadFloat4(InputDataPort port);
        public abstract quaternion ReadQuaternion(InputDataPort port);
        public abstract Entity ReadEntity(InputDataPort port);
        public abstract Value ReadValue(InputDataPort port);
        public abstract Value ReadValueOfType(InputDataPort port, ValueType valueType);
        public abstract bool HasConnectedValue(IOutputDataPort port);
        public abstract bool HasConnectedValue(IOutputTriggerPort port);
        public abstract bool HasConnectedValue(IInputDataPort port);
        public Value GetComponentDefaultValue(TypeReference componentType, int fieldIndex)
        {
            throw new NotImplementedException();
        }

        public Value GetComponentValue(Entity e, TypeReference componentType, int fieldIndex)
        {
            throw new NotImplementedException();
        }

        public void SetComponentValue(Entity e, TypeReference componentType, int fieldIndex, Value value)
        {
            throw new NotImplementedException();
        }
    }

    public class BaseDataNodeRuntimeTests<T> : BaseNodeRuntimeTests<T> where T : struct, IDataNode
    {
        public override void SetUp()
        {
            base.SetUp();
            m_Node = SetupNode(default(T));
        }

        protected void ExecuteNode()
        {
            m_Node.Execute(m_GraphInstanceMock.Object);
        }
    }
    public class BaseFlowNodeRuntimeTests<T> : BaseNodeRuntimeTests<T> where T : struct, IFlowNode
    {
        public override void SetUp()
        {
            base.SetUp();
            m_Node = SetupNode(default(T));
        }

        protected void TriggerPort(InputTriggerPort input)
        {
            m_Node.Execute(m_GraphInstanceMock.Object, input);
        }
    }

    public class BaseFlowNodeStateRuntimeTests<T, TS> : BaseNodeRuntimeTests<T> where T : struct, IFlowNode<TS> where TS : unmanaged, INodeState
    {
        protected void SetupNodeState(in T n) => m_GraphInstanceMock.Object.SetupState(n);
        Execution m_Execution;

        public override void SetUp()
        {
            base.SetUp();
            m_Node = SetupNode(default(T));
            SetupNodeState(m_Node);
        }

        protected void TriggerPort(InputTriggerPort input)
        {
            m_Execution = m_Node.Execute(m_GraphInstanceMock.Object, input);
        }

        protected void UpdateNode()
        {
            if (m_Execution == Execution.Running)
                m_Execution = m_Node.Update(m_GraphInstanceMock.Object);
        }

        protected void AssertNodeIsNotRunning()
        {
            Assert.AreEqual(Execution.Done, m_Execution);
        }

        protected void AssertNodeIsRunning()
        {
            Assert.AreEqual(Execution.Running, m_Execution);
        }
    }
}
