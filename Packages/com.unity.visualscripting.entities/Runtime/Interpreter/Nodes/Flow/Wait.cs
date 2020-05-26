using System;

namespace Runtime
{
    [Serializable]
    public struct Wait : IFlowNode<Wait.State>, INodeReportProgress
    {
        public struct State : INodeState
        {
            public float elapsed;
            public bool running;
        }

        public InputTriggerPort Start;
        public InputTriggerPort Stop;
        public InputTriggerPort Pause;
        public InputTriggerPort Reset;
        [PortDescription("On Done")]
        public OutputTriggerPort OnDone;
        [PortDescription(ValueType.Float)]
        public InputDataPort Duration;

        Execution CheckCompletion<TCtx>(TCtx ctx, ref State state) where TCtx : IGraphInstance
        {
            if (state.elapsed >= ctx.ReadFloat(Duration))
            {
                if (state.running)
                {
                    state.running = false;
                    ctx.Trigger(OnDone);
                }

                return Execution.Done;
            }

            return state.running ? Execution.Running : Execution.Done;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);

            if (port == Start)
            {
                state.running = true;
            }
            else if (port == Reset)
            {
                state.elapsed = 0;
            }
            else if (port == Pause)
            {
                state.running = false;
            }
            else if (port == Stop)
            {
                // Stop = Reset + Pause
                state.elapsed = 0;
                state.running = false;
            }

            return CheckCompletion(ctx, ref state);
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);
            float deltaT = ctx.Time.DeltaTime;
            if (state.running)
            {
                state.elapsed += deltaT;
            }

            return CheckCompletion(ctx, ref state);
        }

        public byte GetProgress<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);
            var seconds = ctx.ReadFloat(Duration);
            return (byte)(seconds <= 0 ? 0 : (byte.MaxValue * state.elapsed / seconds));
        }
    }
}
