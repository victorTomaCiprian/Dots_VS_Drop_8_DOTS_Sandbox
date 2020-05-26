using System;
using Unity.Mathematics;

namespace Runtime
{
    [Serializable]
    public struct MakeFloat3 : IDataNode
    {
        [PortDescription(ValueType.Float)] public InputDataPort X;
        [PortDescription(ValueType.Float)] public InputDataPort Y;
        [PortDescription(ValueType.Float)] public InputDataPort Z;
        [PortDescription(ValueType.Float3)] public OutputDataPort Value;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, new float3(ctx.ReadFloat(X), ctx.ReadFloat(Y), ctx.ReadFloat(Z)));
        }
    }

    [Serializable]
    public struct SplitFloat3 : IDataNode
    {
        [PortDescription(ValueType.Float3)] public InputDataPort Value;
        [PortDescription(ValueType.Float)] public OutputDataPort X;
        [PortDescription(ValueType.Float)] public OutputDataPort Y;
        [PortDescription(ValueType.Float)] public OutputDataPort Z;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var f = ctx.ReadFloat3(Value);
            ctx.Write(X, f.x);
            ctx.Write(Y, f.y);
            ctx.Write(Z, f.z);
        }
    }
}
