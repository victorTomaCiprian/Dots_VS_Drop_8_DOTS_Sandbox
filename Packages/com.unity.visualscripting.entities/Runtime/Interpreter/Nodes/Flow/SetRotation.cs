using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct SetRotation : IFlowNode
    {
        public InputTriggerPort Input;
        public OutputTriggerPort Output;
        [PortDescription(ValueType.Entity)]
        public InputDataPort GameObject;
        [PortDescription(ValueType.Quaternion)]
        public InputDataPort Value;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity != Entity.Null)
            {
                // Make sure that the entity has a Rotation
                if (!ctx.EntityManager.HasComponent<Rotation>(entity))
                    ctx.EntityManager.AddComponent<Rotation>(entity);

                Rotation r = new Rotation { Value = ctx.ReadQuaternion(Value)};
                ctx.EntityManager.SetComponentData(entity, r);
            }

            ctx.Trigger(Output);
        }
    }

    [Serializable]
    public struct GetRotation : IDataNode
    {
        [PortDescription(ValueType.Entity)]
        public InputDataPort GameObject;
        [PortDescription(ValueType.Quaternion)]
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity != Entity.Null)
            {
                if (ctx.EntityManager.HasComponent<Rotation>(entity))
                {
                    Rotation rotation = ctx.EntityManager.GetComponentData<Rotation>(entity);

                    // Not too pretty, but couldn't find a way of getting float3 eulerAngles from quaternion. Shouldn't this be part of the quaternion API?
                    var q = rotation.Value;
                    // float3 euler = new float3(q.eulerAngles.x, q.eulerAngles.y, q.eulerAngles.z);

                    ctx.Write(Value, q);
                }
                else
                {
                    ctx.Write(Value, quaternion.identity);
                }
            }
        }
    }
}
