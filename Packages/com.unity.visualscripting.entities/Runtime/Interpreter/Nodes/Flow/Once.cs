using System;

namespace Runtime
{
    [Serializable]
    public struct Once : IFlowNode<Once.State>
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        public InputTriggerPort Reset;
        [PortDescription("")]
        public OutputTriggerPort Output;

        public struct State : INodeState
        {
            public bool Done;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);
            if (port.Port == Input.Port)
            {
                if (!state.Done)
                {
                    state.Done = true;
                    ctx.Trigger(Output);
                }
            }
            else if (port.Port == Reset.Port)
            {
                state.Done = false;
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
