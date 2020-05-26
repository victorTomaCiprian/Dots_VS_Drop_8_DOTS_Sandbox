using System;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct Clamp : IDataNode
    {
        [PortDescription(ValueType.Float)]
        public InputDataPort Value;
        [PortDescription(ValueType.Float)]
        public InputDataPort Min;
        [PortDescription(ValueType.Float)]
        public InputDataPort Max;
        [PortDescription(ValueType.Float)]
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            float clampedValue = math.clamp(ctx.ReadFloat(Value), ctx.ReadFloat(Min), ctx.ReadFloat(Max));
            ctx.Write(Result, clampedValue);
        }
    }
}
