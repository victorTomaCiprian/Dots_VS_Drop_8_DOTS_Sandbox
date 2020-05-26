using System;
using Runtime.Nodes;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct OnKey : IEntryPointNode, IHasExecutionType<OnKey.KeyEventType>
    {
        public enum KeyEventType
        {
            Down,
            Up,
            Hold
        }

        public KeyCode KeyCode;
        public KeyEventType EventType;

        [PortDescription(ValueType.Bool, DefaultValue = true)]
        public InputDataPort Enabled;
        [PortDescription("")]
        public OutputTriggerPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            if (!ctx.ReadBool(Enabled))
                return;

            if (EventType == KeyEventType.Down && Input.GetKeyDown(KeyCode) ||
                EventType == KeyEventType.Up && Input.GetKeyUp(KeyCode) ||
                EventType == KeyEventType.Hold && Input.GetKey(KeyCode))
                ctx.Trigger(Output);
        }

        public KeyEventType Type
        {
            get { return EventType; }
            set { EventType = value; }
        }
    }
}
