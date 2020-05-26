using System;
using Unity.Entities;
using Unity.Transforms;

namespace Runtime
{
    [Serializable]
    public struct Disable : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        [PortDescription("Instance", ValueType.Entity)]
        public InputDataPort Entity;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);
            if (entity != Unity.Entities.Entity.Null)
                ctx.EntityManager.SetEnabled(entity, false);
            ctx.Trigger(Output);
        }
    }
}
