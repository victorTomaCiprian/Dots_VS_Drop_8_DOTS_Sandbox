using System;

namespace Runtime
{
    [Serializable]
    public struct StopWatch : IFlowNode<StopWatch.WaitState>
    {
        public struct WaitState : INodeState
        {
            public float _elapsed;
        }

        public InputTriggerPort Start;              // Start/Restart the timer
        public InputTriggerPort Stop;               // Stop/Pause the timer
        public InputTriggerPort Reset;              // Reset the timer to 0
        public OutputTriggerPort Output;            // Triggered when Duration is reachd
        public OutputTriggerPort Done;              // Triggered when Duration is reachd
        [PortDescription(ValueType.Float)]
        public InputDataPort Duration;               // Stopwatch duration
        [PortDescription(ValueType.Float)]
        public OutputDataPort Elapsed;              // Time elasped since start
        [PortDescription(ValueType.Float)]
        public OutputDataPort Progress;             // Value from [0,1], where 0 is at reset time, and 1 when reaching Duration

        private Execution CheckCompletion<TCtx>(TCtx ctx, ref WaitState state, bool ForceUpdate, Execution running) where TCtx : IGraphInstance
        {
            Execution Result = running;

            float duration = ctx.ReadFloat(Duration);
            if (state._elapsed >= duration)
            {
                state._elapsed = duration;
                Result = Execution.Done;
            }

            if ((ForceUpdate) || (Result == Execution.Done))
            {
                // Write outputs
                ctx.Write(Elapsed, state._elapsed);
                ctx.Write(Progress, duration > 0f ? state._elapsed / duration : 1f);
                ctx.Trigger(Output);
            }

            // If we are transitionning from running -> Done, Trigger Done
            if ((running == Execution.Running) && (Result == Execution.Done))
            {
                ctx.Log("NodeId.StopWatch Done Trigger");
                ctx.Trigger(Done);
                state._elapsed = 0;
            }

            return Result;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            Execution Result = ctx.IsNodeCurrentlyScheduledForUpdate() ? Execution.Running : Execution.Done;
            ref WaitState state = ref ctx.GetState(this);

            if (port.Triggers(Start))
            {
                ctx.Log("NodeId.StopWatch Start");
                Result = CheckCompletion(ctx, ref state, false, Execution.Running);
            }
            else if (port.Triggers(Reset))
            {
                ctx.Log("NodeId.StopWatch Reset");
                state._elapsed = 0;
                CheckCompletion(ctx, ref state, Result == Execution.Running, Result);
            }
            else // if (port.Triggers(Stop))
            {
                // Nothing special to do - just return Done
                ctx.Log("NodeId.StopWatch Stop");
                Result = Execution.Done;
            }

            return Result;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            Execution Result = Execution.Done;
            ref WaitState state = ref ctx.GetState(this);

            float deltaT = UnityEngine.Time.deltaTime;
            ctx.Log("NodeId.StopWatch Time = " + state._elapsed + " + " + deltaT);
            state._elapsed += deltaT;

            Result = CheckCompletion(ctx, ref state, true, Execution.Running);

            return Result;
        }

        public byte GetProgress<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref WaitState state = ref ctx.GetState(this);
            var duration = ctx.ReadFloat(Duration);
            return (byte)(duration <= 0 ? 0 : (byte.MaxValue * state._elapsed / duration));
        }
    }
}
