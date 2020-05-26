#if VS_DOTS_PHYSICS_EXISTS
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Internal;


namespace Runtime
{
    [Serializable]
    public struct MakeStatic : IFlowNode
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

            if (ctx.EntityManager.HasComponent<PhysicsGravityFactor>(entity))
                ctx.EntityManager.RemoveComponent<PhysicsGravityFactor>(entity);

            if (ctx.EntityManager.HasComponent<PhysicsMass>(entity))
                ctx.EntityManager.RemoveComponent<PhysicsMass>(entity);

            if (ctx.EntityManager.HasComponent<PhysicsVelocity>(entity))
                ctx.EntityManager.RemoveComponent<PhysicsVelocity>(entity);

            if (ctx.EntityManager.HasComponent<PhysicsDamping>(entity))
                ctx.EntityManager.RemoveComponent<PhysicsDamping>(entity);

            ctx.Trigger(Output);
        }
    }

    [Serializable]
    public struct MakeKinematic : IFlowNode
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

            // No mass (infinite)
            if (ctx.EntityManager.HasComponent<PhysicsMass>(entity))
                ctx.EntityManager.RemoveComponent<PhysicsMass>(entity);

            // No damping (animated)
            if (ctx.EntityManager.HasComponent<PhysicsDamping>(entity))
                ctx.EntityManager.RemoveComponent<PhysicsDamping>(entity);

            // No gravity (actually need a component to "remove" the gravity)
            if (!ctx.EntityManager.HasComponent<PhysicsGravityFactor>(entity))
            {
                ctx.EntityManager.AddComponent<PhysicsGravityFactor>(entity);
            }
            ctx.EntityManager.SetComponentData(entity, new PhysicsGravityFactor() {Value = 0});

            // Let's set the Velocity to zero only if it hasn't been set yet (MBriau: not sure if it should always be set to zero or maybe add an option)
            if (!ctx.EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                ctx.EntityManager.AddComponentData(entity, new PhysicsVelocity(){ Linear = float3.zero, Angular = float3.zero});
            }

            ctx.Trigger(Output);
        }
    }

    [Serializable]
    public struct MakeDynamic : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        [PortDescription(ValueType.Float)]
        public InputDataPort Mass;
        [PortDescription(ValueType.Float)]
        public InputDataPort Drag;
        [PortDescription(ValueType.Float)]
        public InputDataPort AngularDrag;
        [PortDescription(ValueType.Float)]
        public InputDataPort GravityFactor;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return;

            PhysicsCollider physicsCollider = ctx.EntityManager.GetComponentData<PhysicsCollider>(entity);
            Collider* colliderPtr = (Collider*)physicsCollider.Value.GetUnsafePtr();

            if (!ctx.EntityManager.HasComponent<PhysicsGravityFactor>(entity))
            {
                ctx.EntityManager.AddComponent<PhysicsGravityFactor>(entity);
            }
            float gravityFactor = ctx.ReadFloat(GravityFactor);
            ctx.EntityManager.SetComponentData(entity, new PhysicsGravityFactor(){Value = gravityFactor});

            if (!ctx.EntityManager.HasComponent<PhysicsMass>(entity))
            {
                ctx.EntityManager.AddComponent<PhysicsMass>(entity);
            }
            float mass = ctx.ReadFloat(Mass);
            ctx.EntityManager.SetComponentData(entity, PhysicsMass.CreateDynamic(colliderPtr->MassProperties, mass));

            if (!ctx.EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                ctx.EntityManager.AddComponent<PhysicsVelocity>(entity);
            }
            ctx.EntityManager.SetComponentData(entity, new PhysicsVelocity {Linear = float3.zero, Angular = float3.zero});

            float drag = ctx.ReadFloat(Drag);
            float angularDrag = ctx.ReadFloat(AngularDrag);
            if (!ctx.EntityManager.HasComponent<PhysicsDamping>(entity))
            {
                ctx.EntityManager.AddComponent<PhysicsDamping>(entity);
            }
            ctx.EntityManager.SetComponentData(entity, new PhysicsDamping {Linear = drag, Angular = angularDrag});

            ctx.Trigger(Output);
        }
    }
}
#endif
