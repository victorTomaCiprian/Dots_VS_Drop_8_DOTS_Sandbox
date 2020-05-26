using System;
using Runtime.Nodes;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    [NodeDescription(InterpolationType.Linear, "Linear interpolation between 2 values within a time interval.")]
    [NodeDescription(InterpolationType.SmoothStep, "Smooth step interpolation between 2 values within a time interval.")]
    public struct Tween : IFlowNode<Tween.State>, IHasExecutionType<InterpolationType>, INodeReportProgress
    {
        public struct State : INodeState
        {
            public float Elapsed;
            public bool IsRunning;
        }

        [PortDescription(Description = "Trigger to start the interpolation.")]
        public InputTriggerPort Start;

        [PortDescription(Description = "Trigger to stop and reset the interpolation.")]
        public InputTriggerPort Stop;

        [PortDescription(Description = "Trigger to pause the interpolation.")]
        public InputTriggerPort Pause;

        [PortDescription(Description = "Resets the internal timer.")]
        public InputTriggerPort Reset;

        [PortDescription(ValueType.Float, Description = "The value to interpolate from.")]
        public InputDataPort From;

        [PortDescription(ValueType.Float, Description = "The value to interpolate to.")]
        public InputDataPort To;

        [PortDescription(ValueType.Float, Description = "The duration of the interpolation (in seconds).")]
        public InputDataPort Duration;

        [PortDescription(Description = "Fires when the interpolation runs to completion (i.e. Stop is not triggered).")]
        public OutputTriggerPort OnDone;

        [PortDescription(Description = "Fires every frame the interpolation runs (i.e. not while paused).")]
        public OutputTriggerPort OnFrame;

        [PortDescription(ValueType.Float, "", DefaultValue = 0f, Description = "The interpolated value between " +
                "To and From at the current time.")]
        public OutputDataPort Result;

        [SerializeField]
        InterpolationType m_Type;

        public InterpolationType Type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);

            if (port == Start)
            {
                state.IsRunning = true;
            }
            else if (port == Stop)
            {
                state.Elapsed = 0;
                state.IsRunning = false;
            }
            else if (port == Pause)
            {
                state.IsRunning = false;
            }
            else if (port == Reset)
            {
                state.Elapsed = 0;
            }

            return CheckCompletion(ctx, ref state);
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);

            if (state.IsRunning)
                state.Elapsed += ctx.Time.DeltaTime;

            return CheckCompletion(ctx, ref state);
        }

        Execution CheckCompletion<TCtx>(TCtx ctx, ref State state) where TCtx : IGraphInstance
        {
            var duration = ctx.ReadFloat(Duration);

            if (state.Elapsed >= duration)
            {
                if (state.IsRunning)
                {
                    ctx.Write(Result, ctx.ReadFloat(To));
                    ctx.Trigger(OnDone);
                }

                return Execution.Done;
            }

            if (state.IsRunning)
            {
                state.Elapsed = math.min(state.Elapsed, duration);

                var progress = duration > 0 ? state.Elapsed / duration : 1.0f;
                var result = Type == InterpolationType.Linear
                    ? math.lerp(ctx.ReadFloat(From), ctx.ReadFloat(To), progress)
                    : Mathf.SmoothStep(ctx.ReadFloat(From), ctx.ReadFloat(To), progress);

                ctx.Write(Result, result);
                ctx.Trigger(OnFrame);
            }

            return state.IsRunning ? Execution.Running : Execution.Done;
        }

        public byte GetProgress<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ref State state = ref ctx.GetState(this);
            var duration = ctx.ReadFloat(Duration);
            return (byte)(duration <= 0 ? 0 : byte.MaxValue * state.Elapsed / duration);
        }
    }
}
