using System;

namespace Runtime
{
    [Serializable]
    public struct RandomInt : IDataNode
    {
        [PortDescription(ValueType.Int)]
        public InputDataPort Min;
        [PortDescription(ValueType.Int, DefaultValue = int.MaxValue)]
        public InputDataPort Max;
        [PortDescription(ValueType.Int)]
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            int minInt = ctx.ReadInt(Min);
            int maxInt = ctx.ReadInt(Max);
            ctx.Write(Result, ctx.Random.NextInt(minInt, maxInt));
        }
    }
}
