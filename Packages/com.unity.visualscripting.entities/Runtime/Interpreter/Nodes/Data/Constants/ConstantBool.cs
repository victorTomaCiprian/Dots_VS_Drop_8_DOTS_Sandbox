using System;

namespace Runtime
{
    [Serializable]
    public struct ConstantBool : IConstantNode<bool>
    {
        public bool Value;

        [PortDescription(ValueType.Bool)]
        public OutputDataPort ValuePort;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(ValuePort, Value);
        }
    }
}
