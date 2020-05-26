using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct SendEvent : IEventDispatcherNode
    {
        public InputTriggerPort Input;
        [PortDescription("Game Object", ValueType.Entity)]
        public InputDataPort Entity;
        public InputDataMultiPort Values;
        public OutputTriggerPort Output;

        [SerializeField]
        ulong m_EventId;

        public ulong EventId
        {
            get => m_EventId;
            set => m_EventId = value;
        }

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var values = new List<Value>();
            for (uint i = 0; i < Values.DataCount; ++i)
            {
                values.Add(ctx.ReadValue(Values.SelectPort(i)));
            }

            if (ctx.HasConnectedValue(Entity))
            {
                var targetEntity = ctx.ReadEntity(Entity);
                if (targetEntity != Unity.Entities.Entity.Null)
                    ctx.DispatchEvent(new DotsEventData(EventId, values, targetEntity));
            }
            else
            {
                ctx.DispatchEvent(new DotsEventData(EventId, values));
            }

            ctx.Trigger(Output);
        }
    }
}
