using System;
using JetBrains.Annotations;

namespace Runtime
{
    [Serializable]
    public struct Enable : IFlowNode
    {
        [UsedImplicitly]
        [PortDescription("")]
        public InputTriggerPort Input;

        [PortDescription("", ValueType.Entity)]
        public InputDataPort Entity;

        [PortDescription("")]
        public OutputTriggerPort Output;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);
            if (entity != Unity.Entities.Entity.Null)
                ctx.EntityManager.SetEnabled(entity, true);
            ctx.Trigger(Output);
        }
    }
}
