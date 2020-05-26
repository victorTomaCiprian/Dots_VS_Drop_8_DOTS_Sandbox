using System;
using System.Linq;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct OnEvent : IEventReceiverNode
    {
        public OutputTriggerPort Output;
        public OutputDataMultiPort Values;

        [SerializeField]
        ulong m_EventId;

        public ulong EventId
        {
            get => m_EventId;
            set => m_EventId = value;
        }

        public Execution Execute<TCtx>(TCtx ctx, DotsEventData data) where TCtx : IGraphInstance
        {
            if (EventId == data.Id)
            {
                for (var i = 0; i < Values.DataCount; ++i)
                {
                    ctx.Write(Values.SelectPort((uint)i), data.Values.ElementAt(i));
                }

                ctx.Trigger(Output);
            }

            return Execution.Done;
        }
    }
}
