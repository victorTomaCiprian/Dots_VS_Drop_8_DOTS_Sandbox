#if VS_DOTS_PHYSICS_EXISTS
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Internal;
using Collider = Unity.Physics.Collider;

namespace Runtime
{
    [Serializable]
    public struct DisableCollisions : IFlowNode<DisableCollisions.State>
    {
        public struct State : INodeState
        {
            public CollisionFilter CollisionFilter;
        }

        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("Re Enable")]
        public InputTriggerPort ReEnable;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Re Enabled")]
        public OutputTriggerPort ReEnabled;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;

        public unsafe Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return Execution.Done;

            if (!ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return Execution.Done;

            ref State state = ref ctx.GetState(this);

            PhysicsCollider physicsCollider = ctx.EntityManager.GetComponentData<PhysicsCollider>(entity);
            Collider* colliderPtr = (Collider*)physicsCollider.Value.GetUnsafePtr();

            if (port == Input)
            {
                CollisionFilter currentFilter = colliderPtr->Filter;

                if (currentFilter.BelongsTo != CollisionFilter.Zero.BelongsTo || currentFilter.CollidesWith != CollisionFilter.Zero.CollidesWith || currentFilter.GroupIndex != CollisionFilter.Zero.GroupIndex)
                {
                    state.CollisionFilter = currentFilter;
                    colliderPtr->Filter = CollisionFilter.Zero;
                }

                ctx.Trigger(Output);
            }
            else if (port == ReEnable)
            {
                CollisionFilter newFilter = state.CollisionFilter;

                if (newFilter.BelongsTo != CollisionFilter.Zero.BelongsTo || newFilter.CollidesWith != CollisionFilter.Zero.CollidesWith || newFilter.GroupIndex != CollisionFilter.Zero.GroupIndex)
                {
                    colliderPtr->Filter = newFilter;
                }

                ctx.Trigger(ReEnabled);
            }

            return Execution.Done;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            return Execution.Done;
        }
    }
}
#endif
