using System;

namespace Runtime
{
    [Serializable]
    public struct OnUpdate : IEntryPointNode
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
