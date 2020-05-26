using System;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct OnDestroy : IEntryPointNode
    {
        [PortDescription(ValueType.Bool, DefaultValue = true)]
        public InputDataPort Enabled;
        [PortDescription("")]
        public OutputTriggerPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            if (ctx.ReadBool(Enabled))
                ctx.Trigger(Output);
        }
    }
}
