using System;
using UnityEditorInternal;

namespace Runtime
{
    [Serializable]
    public struct Switch : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;

        [PortDescription(Description = "The default trigger if none of the switch values match the selector value")]
        public OutputTriggerPort Default;

        [PortDescription(ValueType.Int)]
        public InputDataPort Selector;

        [PortDescription(ValueType.Int)]
        public InputDataMultiPort SwitchValues;

        public OutputTriggerMultiPort SwitchTriggers;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            bool anyMatched = false;
            var val = ctx.ReadInt(Selector);
            for (uint i = 0; i < SwitchValues.DataCount; i++)
            {
                if (ctx.ReadInt(SwitchValues.SelectPort(i)) == val)
                {
                    ctx.Trigger(SwitchTriggers.SelectPort(i));
                    anyMatched = true;
                }
            }
            if (!anyMatched)
                ctx.Trigger(Default);
        }
    }
}
