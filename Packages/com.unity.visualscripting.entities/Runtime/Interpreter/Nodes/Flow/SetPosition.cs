using System;
using Unity.Entities;
using Unity.Transforms;

namespace Runtime
{
    [Serializable]
    public struct SetPosition : IFlowNode
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
                var t = ctx.EntityManager.GetComponentData<Translation>(entity);
                t.Value = ctx.ReadFloat3(Value);
                ctx.EntityManager.SetComponentData(entity, t);
            }

            ctx.Trigger(Output);
        }
    }

    [Serializable]
    public struct GetPosition : IDataNode
    {
        [PortDescription(ValueType.Entity)]
        public InputDataPort GameObject;
        [PortDescription(ValueType.Float3)]
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity != Entity.Null)
            {
                var t = ctx.EntityManager.GetComponentData<Translation>(entity);
                ctx.Write(Value, t.Value);
            }
        }
    }
}
