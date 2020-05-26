using System;

namespace Runtime
{
    [Serializable]
    public struct RandomFloat : IDataNode
    {
        [PortDescription(ValueType.Float)] public InputDataPort Min;
        [PortDescription(ValueType.Float, DefaultValue = 1.0f)] public InputDataPort Max;
        [PortDescription(ValueType.Float)] public OutputDataPort Result;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            float minFloat = ctx.ReadFloat(Min);
            float maxFloat = ctx.ReadFloat(Max);
            ctx.Write(Result, ctx.Random.NextFloat(minFloat, maxFloat));
        }
    }
}
