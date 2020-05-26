using System;
using Unity.Mathematics;

namespace Runtime
{
    [Serializable]
    public struct ConstantQuaternion : IConstantNode<quaternion>
    {
        public quaternion Value;

        [PortDescription(ValueType.Quaternion)]
        public OutputDataPort ValuePort;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(ValuePort, Value);
        }
    }
}
