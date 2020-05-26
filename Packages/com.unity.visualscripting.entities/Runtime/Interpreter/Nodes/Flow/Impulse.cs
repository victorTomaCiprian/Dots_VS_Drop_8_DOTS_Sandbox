#if VS_DOTS_PHYSICS_EXISTS
using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;


namespace Runtime
{
    [Serializable]
    public struct SimpleImpulse : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        [PortDescription(ValueType.Float3)]
        public InputDataPort Value;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsVelocity>(entity) || !ctx.EntityManager.HasComponent<PhysicsVelocity>(entity))
                return;

            PhysicsVelocity physicsVelocity = ctx.EntityManager.GetComponentData<PhysicsVelocity>(entity);
            PhysicsMass physicsMass = ctx.EntityManager.GetComponentData<PhysicsMass>(entity);

            physicsVelocity.ApplyLinearImpulse(physicsMass, ctx.ReadFloat3(Value));

            ctx.EntityManager.SetComponentData(entity, physicsVelocity);
        }
    }

    [Serializable]
    public struct Impulse : IFlowNode
    {
        // We can avoid a "SimpleImpulse" and only use this one if this is clear enough. It could be interesting to gray out the Point port when this is true...
        public bool LinearOnly;

        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        [PortDescription(ValueType.Float3)]
        public InputDataPort Value;
        [PortDescription(ValueType.Float3)]
        public InputDataPort Point;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsMass>(entity) || !ctx.EntityManager.HasComponent<PhysicsVelocity>(entity))
                return;

            PhysicsMass physicsMass = ctx.EntityManager.GetComponentData<PhysicsMass>(entity);
            PhysicsVelocity physicsVelocity = ctx.EntityManager.GetComponentData<PhysicsVelocity>(entity);

            if (LinearOnly)
            {
                physicsVelocity.ApplyLinearImpulse(physicsMass, ctx.ReadFloat3(Value));
            }
            else
            {
                Translation t = ctx.EntityManager.GetComponentData<Translation>(entity);
                Rotation r = ctx.EntityManager.GetComponentData<Rotation>(entity);

                ComponentExtensions.ApplyImpulse(ref physicsVelocity, physicsMass, t, r, ctx.ReadFloat3(Value), ctx.ReadFloat3(Point));
            }

            ctx.EntityManager.SetComponentData(entity, physicsVelocity);
        }
    }
}

#endif
