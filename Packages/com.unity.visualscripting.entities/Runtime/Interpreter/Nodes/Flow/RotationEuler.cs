using System;
using Unity.Mathematics;

namespace Runtime
{
    [Serializable]
    public struct RotationEuler : IDataNode
    {
        [PortDescription(ValueType.Float3, Description = "Euler angles in degrees")]
        public InputDataPort Euler;
        [PortDescription(ValueType.Quaternion)]
        public OutputDataPort Value;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Value, quaternion.Euler(math.radians(ctx.ReadFloat3(Euler))));
        }
    }
}
