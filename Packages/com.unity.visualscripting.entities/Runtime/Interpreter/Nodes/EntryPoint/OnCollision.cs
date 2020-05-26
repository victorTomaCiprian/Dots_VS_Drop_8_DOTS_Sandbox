#if VS_DOTS_PHYSICS_EXISTS
using System;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using VisualScripting.Physics;

namespace Runtime
{
    [Serializable]
    public struct OnCollision : IEventReceiverNode
    {
        [PortDescription(ValueType.Bool, DefaultValue = true)]
        public InputDataPort Enabled;

        [PortDescription(ValueType.Entity)]
        public InputDataPort Instance;

        public OutputTriggerPort Entered;
        public OutputTriggerPort Exited;
        public OutputTriggerPort Inside;

        [PortDescription(ValueType.Entity)]
        public OutputDataPort Detected;

        [SerializeField]
        ulong m_EventId;

        public ulong EventId
        {
            get => m_EventId;
            set => m_EventId = value;
        }

        public Execution Execute<TCtx>(TCtx ctx, DotsEventData data) where TCtx : IGraphInstance
        {
            if (!ctx.ReadBool(Enabled))
                return Execution.Done;

            var detected = ctx.ReadEntity(Instance);
            var other = data.Values.ElementAt(0).Entity;
            if (detected != Entity.Null && detected != other)
                return Execution.Done;

            ctx.Write(Detected, other);

            var state = (VisualScriptingPhysics.CollisionState)data.Values.ElementAt(1).Int;
            switch (state)
            {
                case VisualScriptingPhysics.CollisionState.None:
                    break;
                case VisualScriptingPhysics.CollisionState.Enter:
                    ctx.Trigger(Entered);
                    ctx.Trigger(Inside);
                    break;
                case VisualScriptingPhysics.CollisionState.Stay:
                    ctx.Trigger(Inside);
                    break;
                case VisualScriptingPhysics.CollisionState.Exit:
                    ctx.Trigger(Exited);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Execution.Done;
        }
    }
}
#endif
