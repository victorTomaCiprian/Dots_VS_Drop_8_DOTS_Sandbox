using System;
using UnityEngine;

namespace Runtime
{
    // Node that inspects some data every frame and will fire its output pin when the observed data changes.
    [Serializable]
    public struct OnChange : IFlowNode<OnChange.State>
    {
        public struct State : INodeState
        {
            public Value LastValue;
        }

        public InputTriggerPort Start;          // The signal to start observing the data.
        public InputTriggerPort Stop;           // The signal to stop observing the data.
        [PortDescription("")]
        public OutputTriggerPort OnChanged;      // Fires when the observed data changed.
        public InputDataPort Input;          // The data to observe.

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            Execution Result = Execution.Done;
            ref State state = ref ctx.GetState(this);

            if (port.Triggers(Start))
            {
                ctx.Log("NodeId.OnChange StartObserving");
                Result = Execution.Running;
                state.LastValue = ctx.ReadValue(Input);
            }
            else
            {
                ctx.Log("NodeId.StopWatch Stop");
            }

            return Result;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);

            Runtime.Value Temp = ctx.ReadValue(Input);
            if (!Temp.Equals(state.LastValue))
                ctx.Trigger(OnChanged);
            state.LastValue = Temp;

            return Execution.Running;
        }
    }
}
