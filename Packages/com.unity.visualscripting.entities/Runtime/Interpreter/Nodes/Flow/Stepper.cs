using System;
using Unity.Mathematics;

namespace Runtime
{
    /// <summary>
    /// node that takes a single input trigger and that will fire a different user defined output, sequentially, every time an input signal is received to
    /// that I can control the executions of a sequence of node.
    /// For example, given the user defined output 1, 2 and 3:
    ///
    /// - Upon getting the input a first time, the node would fire output 1
    /// - Upon getting the input a second time, the node would fire output 2
    /// - Upon getting the input a third time, the node would fire output 3
    /// - Upon getting the input a fourth time, the node would fire output 1 again
    ///
    /// Expected execution with Hold:
    ///     1, 2, 3, 3, (...), 3
    ///
    /// Expected execution with Ping Pong:
    ///     1, 2, 3, 2, 1, 2, 3, 2, 1, ...
    /// </summary>
    [Serializable]
    public struct Stepper : IFlowNode<Stepper.State>
    {
        public struct State : INodeState
        {
            public uint _index;
        }

        public enum OrderMode
        {
            Hold, Loop, PingPong
        }

        public OrderMode Mode;

        public InputTriggerPort In;
        public InputTriggerPort Reset;
        public OutputTriggerMultiPort Step;

        public uint MaxStepIndex => (uint)Step.GetDataCount();

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);

            if (port.Triggers(In))
            {
                ctx.Log($"NodeId.Sequence In - internal index: {state._index}");
                switch (Mode)
                {
                    case OrderMode.Hold:
                        ctx.Trigger(Step.SelectPort(state._index));
                        state._index = math.min(state._index + 1, MaxStepIndex - 1);
                        break;
                    case OrderMode.Loop:
                        ctx.Trigger(Step.SelectPort(state._index));
                        state._index = (state._index + 1) % MaxStepIndex;
                        break;
                    case OrderMode.PingPong:
                        var index = state._index;
                        if (index >= MaxStepIndex)
                            index = (MaxStepIndex - 2) - (index % MaxStepIndex);
                        ctx.Trigger(Step.SelectPort(index));
                        state._index = (state._index + 1) % (MaxStepIndex * 2 - 2);
                        break;
                }
            }
            else // Reset
            {
                ctx.Log("NodeId.Sequence Reset");
                state._index = 0;
            }

            return Execution.Done;;
        }

        // Update should never be called
        public Execution Update<TCtx>(TCtx _) where TCtx : IGraphInstance
        {
            return Execution.Done;
        }
    }
}
