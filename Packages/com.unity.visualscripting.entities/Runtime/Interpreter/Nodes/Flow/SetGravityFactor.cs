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
    public struct SetGravityFactor : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;
        [PortDescription(ValueType.Float)]
        public InputDataPort Value;

        public unsafe void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);

            if (entity == Unity.Entities.Entity.Null)
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsCollider>(entity))
                return;

            if (!ctx.EntityManager.HasComponent<PhysicsGravityFactor>(entity))
                ctx.EntityManager.AddComponent<PhysicsGravityFactor>(entity);

            float gravityFactor = ctx.ReadFloat(Value);

            ctx.EntityManager.SetComponentData(entity, new PhysicsGravityFactor()
            {
                Value = gravityFactor
            });

            ctx.Trigger(Output);
        }
    }

    [Serializable]
    public struct GetGravityFactor : IDataNode
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
                if (ctx.EntityManager.HasComponent<PhysicsGravityFactor>(entity))
                {
                    PhysicsGravityFactor physicsGravityFactor = ctx.EntityManager.GetComponentData<PhysicsGravityFactor>(entity);

                    ctx.Write(Value, physicsGravityFactor.Value);
                }
                else
                {
                    ctx.Write(Value, 1.0f);
                }
            }
        }
    }
}

#endif
