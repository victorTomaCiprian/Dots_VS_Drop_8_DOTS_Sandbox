using System;
using Runtime.Nodes;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct Time : IDataNode, IHasExecutionType<Time.TimeType>
    {
        public enum TimeType
        {
            DeltaTime,
            ElapsedTime,
            FrameCount,
        }
        [PortDescription(ValueType.Float)]
        public OutputDataPort Value;
        [SerializeField]
        TimeType m_Type;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            switch (m_Type)
            {
                case TimeType.DeltaTime:
                    ctx.Write(Value, ctx.Time.DeltaTime);
                    break;
                case TimeType.ElapsedTime:
                    ctx.Write(Value, (float)ctx.Time.ElapsedTime);
                    break;
                case TimeType.FrameCount:
                    // TODO why ain't that available in DOTS ?
                    ctx.Write(Value, UnityEngine.Time.frameCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public TimeType Type
        {
            get => m_Type;
            set => m_Type = value;
        }
    }
}
