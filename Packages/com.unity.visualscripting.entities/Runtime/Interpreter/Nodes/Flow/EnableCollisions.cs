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
    public struct EnableCollisions : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return;

            PhysicsCollider physicsCollider = ctx.EntityManager.GetComponentData<PhysicsCollider>(entity);
            Collider* colliderPtr = (Collider*)physicsCollider.Value.GetUnsafePtr();

            colliderPtr->Filter = CollisionFilter.Default;

            ctx.Trigger(Output);
        }
    }

    // MBRIAU: Not exposed yet
    [Serializable]
    public struct SetCollisionFilter : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        // MBRIAU: Would need to use UInt
        [PortDescription(ValueType.Int)]
        public InputDataPort BelongsTo;
        // MBRIAU: Would need to use UInt
        [PortDescription(ValueType.Int)]
        public InputDataPort CollidesWith;
        [PortDescription(ValueType.Int)]
        public InputDataPort GroupIndex;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return;

            PhysicsCollider physicsCollider = ctx.EntityManager.GetComponentData<PhysicsCollider>(entity);
            Collider* colliderPtr = (Collider*)physicsCollider.Value.GetUnsafePtr();

            CollisionFilter newFilter = new CollisionFilter();

            newFilter.BelongsTo = (uint)ctx.ReadInt(this.BelongsTo);
            newFilter.CollidesWith = (uint)ctx.ReadInt(this.CollidesWith);
            newFilter.GroupIndex = ctx.ReadInt(this.GroupIndex);

            colliderPtr->Filter = newFilter;

            ctx.Trigger(Output);
        }
    }
}
#endif
