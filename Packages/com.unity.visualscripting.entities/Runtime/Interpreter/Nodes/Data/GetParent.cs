using System;
using Unity.Entities;
using Unity.Transforms;

namespace Runtime
{
    [Serializable]
    public struct GetParent : IDataNode
    {
        [PortDescription(ValueType.Entity, "")]
        public InputDataPort GameObject;

        [PortDescription(ValueType.Entity, "")]
        public OutputDataPort Parent;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var go = ctx.ReadEntity(GameObject);
            var result = Entity.Null;

            if (go != Entity.Null && ctx.EntityManager.HasComponent<Parent>(go))
            {
                var parent = ctx.EntityManager.GetComponentData<Parent>(go);
                result = parent.Value;
            }

            ctx.Write(Parent, result);
        }
    }
}
