using System;

namespace Runtime
{
    [Serializable]
    public struct If : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;

        [PortDescription(ValueType.Bool, "")]
        public InputDataPort Condition;

        [PortDescription("True")]
        public OutputTriggerPort IfTrue;

        [PortDescription("False")]
        public OutputTriggerPort IfFalse;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ctx.Trigger(ctx.ReadBool(Condition) ? IfTrue : IfFalse);
        }
    }
}
