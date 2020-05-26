#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define VS_TRACING
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Assertions;
#if VS_DOTS_PHYSICS_EXISTS
using VisualScripting.Physics;
#endif

// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Runtime
{
    public struct ScriptingGraphInstance : ISystemStateComponentData {}
    public struct ScriptingGraphInstanceAlive : IComponentData {}

    [InternalBufferCapacity(8)]
    public struct ValueInput : IBufferElementData
    {
        public uint Index;
        public Value Value;
    }

    public struct ScriptingGraph : ISharedComponentData, IEquatable<ScriptingGraph>
    {
        public ScriptingGraphAsset ScriptingGraphAsset;

        public bool Equals(ScriptingGraph other)
        {
            return Equals(ScriptingGraphAsset, other.ScriptingGraphAsset);
        }

        public override bool Equals(object obj)
        {
            return obj is ScriptingGraph other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ScriptingGraphAsset != null ? ScriptingGraphAsset.GetHashCode() : 0;
        }
    }

    public class ScriptingGraphRuntime : ComponentSystem
    {
#if VS_DOTS_PHYSICS_EXISTS
        NativeMultiHashMap<Entity, VisualScriptingPhysics.CollisionTriggerData> m_TriggerData;
        NativeMultiHashMap<Entity, VisualScriptingPhysics.CollisionTriggerData> m_CollisionData;
#endif

        Dictionary<Entity, GraphInstance> m_Contexts;
        EntityQuery m_UninitializedQuery;
        EntityQuery m_Query;
        EntityQueryMask m_BeingDestroyedQueryMask;

        // TODO We should compare events with TypeId (int) just like the UIElement's eventDispatcher
        List<DotsEventData> m_DispatchedEvents = new List<DotsEventData>();

#if UNITY_EDITOR // Live edit
        public static int LastVersion;
        private int m_Version;
#endif

        protected override void OnCreate()
        {
            m_Contexts = new Dictionary<Entity, GraphInstance>();
            m_UninitializedQuery = GetEntityQuery(typeof(ScriptingGraph), ComponentType.Exclude<ScriptingGraphInstance>());
            m_Query = GetEntityQuery(typeof(ScriptingGraphInstance));
            var beingDestroyedQuery = GetEntityQuery(typeof(ScriptingGraphInstance), ComponentType.Exclude<ScriptingGraphInstanceAlive>());
            m_BeingDestroyedQueryMask = EntityManager.GetEntityQueryMask(beingDestroyedQuery);

#if VS_DOTS_PHYSICS_EXISTS
            // TODO A FULL HUNDRED
            m_TriggerData = new NativeMultiHashMap<Entity, VisualScriptingPhysics.CollisionTriggerData>(100, Allocator.Persistent);
            m_CollisionData = new NativeMultiHashMap<Entity, VisualScriptingPhysics.CollisionTriggerData>(100, Allocator.Persistent);
#endif
        }

        protected override void OnDestroy()
        {
#if VS_DOTS_PHYSICS_EXISTS
            m_TriggerData.Dispose();
            m_CollisionData.Dispose();
#endif
            foreach (var keyValuePair in m_Contexts)
            {
                keyValuePair.Value?.Dispose();
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
#if UNITY_EDITOR // Live edit
            if (m_Version != LastVersion)
            {
                m_Version = LastVersion;
                foreach (var contextsValue in m_Contexts.Values)
                    contextsValue?.Dispose();
                m_Contexts.Clear();
                EntityManager.RemoveComponent<ScriptingGraphInstance>(m_Query);
            }
#endif

            Entities.With(m_UninitializedQuery).ForEach((Entity e, ScriptingGraph g) =>
            {
                // Start
                var inputs = EntityManager.HasComponent<ValueInput>(e)
                    ? EntityManager.GetBuffer<ValueInput>(e)
                    : new DynamicBuffer<ValueInput>();
                GraphInstance ctx = CreateEntityContext(inputs, e, g.ScriptingGraphAsset.Definition);
#if VS_TRACING
                ctx.ScriptingGraphAssetID = g.ScriptingGraphAsset.GetInstanceID();
#endif
                EntityManager.AddComponentData(e, new ScriptingGraphInstance());
                EntityManager.AddComponentData(e, new ScriptingGraphInstanceAlive());
#if !UNITY_EDITOR // keep it for live edit
                EntityManager.RemoveComponent<ScriptingGraph>(e);
#endif
            });

#if VS_DOTS_PHYSICS_EXISTS
            VisualScriptingPhysics.SetupCollisionTriggerData(EntityManager, ref m_TriggerData, m_Query, VisualScriptingPhysics.EventType.Trigger);
            VisualScriptingPhysics.SetupCollisionTriggerData(EntityManager, ref m_CollisionData, m_Query, VisualScriptingPhysics.EventType.Collision);
#endif
            // A list: I assume the most common case is "the entity has not been destroyed"
            NativeList<Entity> destroyed = new NativeList<Entity>(Allocator.Temp);
            Entities.With(m_Query).ForEach((Entity e) =>
            {
                var beingDestroyed = m_BeingDestroyedQueryMask.Matches(e);

                GraphInstance ctx;

                //Get the context
                if (beingDestroyed)
                {
                    if (!m_Contexts.TryGetValue(e, out ctx))
                        return;
                    destroyed.Add(e);
                }
                else
                {
                    if (!m_Contexts.TryGetValue(e, out ctx))
                    {
                        // MBRIAU: Should this be an error?
                        return;
                    }
                }

                ctx.LastSystemVersion = LastSystemVersion;
                ctx.ResetFrame(); // TODO move at the end

#if VS_TRACING
                if (ctx.FrameTrace == null)
                    ctx.FrameTrace = new DotsFrameTrace(Allocator.Persistent);
#endif

                if (beingDestroyed)
                {
                    ctx.TriggerEntryPoints<OnDestroy>();
                    ctx.RunFrame(e, Time);

                    EntityManager.RemoveComponent<ScriptingGraphInstance>(e);
                    EntityManager.RemoveComponent<ScriptingGraph>(e);
                }
                else
                {
                    // Start
                    if (ctx.IsStarting)
                    {
                        ctx.TriggerEntryPoints<OnStart>();
                        ctx.IsStarting = false;
                    }

                    // Update
                    ctx.TriggerEntryPoints<OnUpdate>();
                    ctx.TriggerEntryPoints<OnKey>();

#if VS_DOTS_PHYSICS_EXISTS
                    TriggerPhysicsEvents(e, ref m_TriggerData, VisualScriptingPhysics.TriggerEventId);
                    TriggerPhysicsEvents(e, ref m_CollisionData, VisualScriptingPhysics.CollisionEventId);
#endif
                    // Actually execute all nodes active
                    ctx.RunFrame(e, Time);

                    m_DispatchedEvents.AddRange(ctx.DispatchedEvents);
                }
            });

            TriggerEvents(m_DispatchedEvents);

#if VS_TRACING
            foreach (var graphInstancePair in m_Contexts)
            {
                var graphInstance = graphInstancePair.Value;
                if (graphInstance?.FrameTrace != null)
                {
                    DotsFrameTrace.FlushFrameTrace(graphInstance.ScriptingGraphAssetID, UnityEngine.Time.frameCount,
                        graphInstance.CurrentEntity,
#if UNITY_EDITOR
                        EntityManager.GetName(graphInstance.CurrentEntity),
#else
                        e.Index.ToString(),
#endif
                        graphInstance.FrameTrace);
                    graphInstance.FrameTrace = null;
                }
            }
#endif
            for (var index = 0; index < destroyed.Length; index++)
            {
                var entity = destroyed[index];
                m_Contexts[entity].Dispose();
                m_Contexts.Remove(entity);
            }

            destroyed.Dispose();
        }

        private struct ParentContext
        {
            public GraphReference CallingNode;
            public GraphInstance ParentGraphInstance;
            public bool IsValid => ParentGraphInstance != null;
        }

        ParentContext _parentContext;

        public Execution RunNestedGraph(GraphInstance graphInstance, ref GraphInstance.ActiveNodesState currentExecutionSavedState, GraphReference graphReference, Entity target, int triggerIndex)
        {
            // we're past matching the beingDestroyedQuery as ScriptingGraphInstance and all have already been removed
            if (!EntityManager.Exists(target) || !m_Contexts.TryGetValue(target, out GraphInstance otherContext))
            {
                Debug.LogError($"Running nested graph on destroyed entity: {target}, aborting");
                return Execution.Done;
            }
            _parentContext.CallingNode = graphReference;
            _parentContext.ParentGraphInstance = graphInstance;
            Assert.AreNotEqual(otherContext, graphInstance, "RunNested graph has been called with identical parent and child context");

            var savedUpdateState = otherContext.SaveActiveNodesState();
            if (currentExecutionSavedState.NodesToExecute == null)
                currentExecutionSavedState.Init();// fetch from current node exec.SavedState
            currentExecutionSavedState.MoveStateToNextFrame();
            otherContext.RestoreActiveNodesState(currentExecutionSavedState);

            if (triggerIndex != -1)
            {
                GraphDefinition.InputOutputTrigger nestedInputTrigger = otherContext.GetInputTrigger(triggerIndex);
                currentExecutionSavedState.AddExecutionThisFrame(nestedInputTrigger.NodeId);
            }

            bool nodesLeftToRun = true;
            while (nodesLeftToRun)
            {
                nodesLeftToRun = otherContext.ResumeFrame(target, Time, default);

                // copy the nested graph outputs to the reference node data output port. it used to be done when triggering a graph output,
                // but not all nested graphs have a output trigger, and calling graphs might just rely on data outputs.
                for (uint i = 0; i < _parentContext.CallingNode.DataOutputs.DataCount; i++)
                    _parentContext.ParentGraphInstance.Write(_parentContext.CallingNode.DataOutputs.SelectPort(i), otherContext.ReadGraphOutputValue((int)i));

                // add graphref to stack ?
                graphInstance.ResumeFrame(graphInstance.CurrentEntity, Time, default);
            }
            _parentContext = default;
            var stillRunning = currentExecutionSavedState.AnyNodeNextFrame;
            otherContext.RestoreActiveNodesState(savedUpdateState);

            return stillRunning ? Execution.Running : Execution.Done;
        }

        public Execution TriggerGraphOutput(GraphInstance nestedContext, uint outputIndex)
        {
            Assert.IsNotNull(_parentContext.ParentGraphInstance);

            _parentContext.ParentGraphInstance.Trigger(_parentContext.CallingNode.Outputs.SelectPort(outputIndex));
            return Execution.Interrupt;
        }

        internal GraphInstance CreateEntityContext(DynamicBuffer<ValueInput> inputs,
            Entity e,
            GraphDefinition graphDefinition)
        {
            Unity.Assertions.Assert.AreNotEqual(Entity.Null, e);
            GraphInstance ctx;
            var graphContext = ctx = GraphInstance.Create(graphDefinition, EntityManager, inputs);
            ctx.ScriptingGraphRuntime = this;
            m_Contexts.Add(e, graphContext);
            foreach (var subgraphReference in graphDefinition.SubgraphReferences)
            {
                Value subgraphEntity = graphContext.ReadDataSlot(subgraphReference.SubgraphEntityDataIndex);
                CreateEntityContext(default,
                    subgraphEntity.Entity,
                    subgraphReference.Subgraph.Definition);
            }
            return ctx;
        }

        void TriggerEvents(List<DotsEventData> events)
        {
            while (events.Count > 0)
            {
                var dispatchedEvents = new List<DotsEventData>();

                for (var i = 0; i < events.Count; ++i)
                {
                    var evt = m_DispatchedEvents[i];

                    Entities.ForEach((Entity e, ScriptingGraph g) =>
                    {
                        if (!m_Contexts.TryGetValue(e, out var ctx) || ctx == null)
                            return;

                        ctx.ResetFrame();
#if VS_DOTS_PHYSICS_EXISTS
                        if (evt.Id == VisualScriptingPhysics.TriggerEventId)
                            ctx.TriggerEvents<OnTrigger>();
                        else if (evt.Id == VisualScriptingPhysics.CollisionEventId)
                            ctx.TriggerEvents<OnCollision>();
                        else
#endif
                        ctx.TriggerEvents<OnEvent>();
                        ctx.RunFrame(e, Time, evt);
                        dispatchedEvents.AddRange(ctx.DispatchedEvents);
                    });

                    events.RemoveAt(i);
                    i--;
                }

                events.AddRange(dispatchedEvents);
            }
        }

#if VS_DOTS_PHYSICS_EXISTS
        void TriggerPhysicsEvents(Entity e,
            ref NativeMultiHashMap<Entity, VisualScriptingPhysics.CollisionTriggerData> data,
            ulong eventId)
        {
            if (data.ContainsKey(e))
            {
                var collectedData = VisualScriptingPhysics.CollectCollisionTriggerData(ref data, e);
                if (collectedData != null)
                {
                    foreach (var values in collectedData.Value.Select(t => new List<Value> { t.Other, (int)t.State }))
                    {
                        m_DispatchedEvents.Add(new DotsEventData(eventId, values));
                    }
                }
                collectedData?.Dispose();
            }
        }

#endif

        public Value ReadGraphInputValue(int graphInputIndex)
        {
            Assert.IsTrue(_parentContext.IsValid, "Cannot read a graph data input value if the parent context data is invalid");
            var value = _parentContext.ParentGraphInstance.ReadValue(_parentContext.CallingNode.DataInputs.SelectPort((uint)graphInputIndex));
            return value;
        }
    }
}
