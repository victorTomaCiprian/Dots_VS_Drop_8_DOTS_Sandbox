using System;

namespace Runtime
{
    [Serializable]
    public struct WaitForAll : IFlowNode<WaitForAll.State>
    {
        [PortDescription("")]
        public InputTriggerMultiPort Input;
        public InputTriggerPort Reset;
        public OutputTriggerPort Output;


        public struct State : INodeState
        {
            public ulong Done;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);

            if (port == Reset)
            {
                state.Done = 0ul;
                return Execution.Done;
            }

            int portIndex = ctx.GetTriggeredIndex(Input, port);
            state.Done |= 1ul << portIndex;

            if (state.Done == (1ul << Input.DataCount) - 1ul)
            {
                ctx.Trigger(Output);
                state.Done = 0ul;
            }

            return Execution.Done;
        }

        // Update should never be called
        public Execution Update<TCtx>(TCtx _) where TCtx : IGraphInstance
        {
            return Execution.Done;
        }
    }
}
