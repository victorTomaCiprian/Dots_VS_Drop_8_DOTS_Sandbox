#if VS_DOTS_PHYSICS_EXISTS
using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;


namespace Runtime
{
    [Serializable]
    public struct SetMass : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        [PortDescription(ValueType.Float)]
        public InputDataPort Mass;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsMass>(entity) || !ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return;

            PhysicsCollider physicsCollider = ctx.EntityManager.GetComponentData<PhysicsCollider>(entity);

            float newMass = ctx.ReadFloat(Mass);

            Collider* colliderPtr = (Collider*)physicsCollider.Value.GetUnsafePtr();

            ctx.EntityManager.SetComponentData(entity, PhysicsMass.CreateDynamic(colliderPtr->MassProperties, newMass));
        }
    }

    [Serializable]
    public struct GetMass : IDataNode
    {
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort GameObject;
        [PortDescription(ValueType.Float)]
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity != Entity.Null)
            {
                if (ctx.EntityManager.HasComponent<PhysicsMass>(entity))
                {
                    PhysicsMass physicsMass = ctx.EntityManager.GetComponentData<PhysicsMass>(entity);

                    ctx.Write(Value, 1.0f / physicsMass.InverseMass);
                }
                else
                {
                    ctx.Write(Value, 0);
                }
            }
        }
    }
}

#endif
