using System;
using Unity.Entities;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct Instantiate : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;

        [PortDescription(ValueType.Entity, "")]
        public InputDataPort Prefab;

        [PortDescription("")]
        public OutputTriggerPort Output;

        [PortDescription(ValueType.Entity, "")]
        public OutputDataPort Instantiated;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Prefab);

            if (entity != Entity.Null)
            {
                var instantiated = ctx.EntityManager.Instantiate(entity);
                ctx.Write(Instantiated, instantiated);
            }

            ctx.Trigger(Output);
        }
    }
}
