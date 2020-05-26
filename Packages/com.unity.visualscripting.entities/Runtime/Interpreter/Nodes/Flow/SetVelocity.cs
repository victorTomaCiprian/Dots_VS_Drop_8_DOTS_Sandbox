#if VS_DOTS_PHYSICS_EXISTS
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;


namespace Runtime
{
    [Serializable]
    public struct SetVelocities : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        [PortDescription(ValueType.Float3)]
        public InputDataPort Linear;
        [PortDescription(ValueType.Float3)]
        public InputDataPort Angular;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsVelocity>(entity))
                return;

            PhysicsCollider physicsCollider = ctx.EntityManager.GetComponentData<PhysicsCollider>(entity);

            Collider* colliderPtr = (Collider*)physicsCollider.Value.GetUnsafePtr();

            float3 linearVelocity = ctx.ReadFloat3(Linear);
            float3 angularVelocity = ctx.ReadFloat3(Angular);

            // TODO: MBRIAU: Make sure to understand exactly what's going on here
            // Calculate the angular velocity in local space from rotation and world angular velocity
            float3 angularVelocityLocal = math.mul(math.inverse(colliderPtr->MassProperties.MassDistribution.Transform.rot), angularVelocity);

            ctx.EntityManager.SetComponentData(entity, new PhysicsVelocity()
            {
                Linear = linearVelocity,
                Angular = angularVelocityLocal
            });

            ctx.Trigger(Output);
        }
    }

    [Serializable]
    public struct GetVelocities : IDataNode
    {
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort GameObject;
        [PortDescription(ValueType.Float3)]
        public OutputDataPort Linear;
        [PortDescription(ValueType.Float3)]
        public OutputDataPort Angular;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity != Entity.Null)
            {
                if (ctx.EntityManager.HasComponent<PhysicsMass>(entity))
                {
                    PhysicsVelocity physicsVelocity = ctx.EntityManager.GetComponentData<PhysicsVelocity>(entity);

                    ctx.Write(Linear, physicsVelocity.Linear);
                    ctx.Write(Angular, physicsVelocity.Angular);
                }
                else
                {
                    ctx.Write(Linear, float3.zero);
                    ctx.Write(Angular, float3.zero);
                }
            }
        }
    }
}

#endif
