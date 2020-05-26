using System;

namespace Runtime
{
    [Serializable]
    public struct StateSwitch : IFlowNode
    {
        [PortDescription("Set True")]
        public InputTriggerPort SetTrue;
        [PortDescription("Set False")]
        public InputTriggerPort SetFalse;
        [PortDescription("")]
        public OutputTriggerPort Done;
        [PortDescription(ValueType.Bool, name: "State", DefaultValue = false)]
        public OutputDataPort State;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ctx.Write(State, port.GetPort().Index == SetTrue.GetPort().Index);
            ctx.Trigger(Done);
        }
    }
}
