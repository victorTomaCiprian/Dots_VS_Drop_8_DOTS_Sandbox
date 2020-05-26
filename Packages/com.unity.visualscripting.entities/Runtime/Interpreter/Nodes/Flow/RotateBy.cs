using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct RotateBy : IFlowNode
    {
        public InputTriggerPort Input;
        public OutputTriggerPort Output;
        [PortDescription(ValueType.Entity)]
        public InputDataPort GameObject;
        [PortDescription(ValueType.Float3)]
        public InputDataPort Value;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity != Entity.Null)
            {
                quaternion currentRotation;
                // Make sure that the entity has a Rotation
                if (!ctx.EntityManager.HasComponent<Rotation>(entity))
                {
                    ctx.EntityManager.AddComponent<Rotation>(entity);
                    currentRotation = quaternion.identity;
                }
                else
                {
                    currentRotation = ctx.EntityManager.GetComponentData<Rotation>(entity).Value;
                }

                currentRotation *= Quaternion.Euler(ctx.ReadFloat3(Value));
                ctx.EntityManager.SetComponentData(entity, new Rotation {Value = currentRotation});
            }

            ctx.Trigger(Output);
        }
    }
}
