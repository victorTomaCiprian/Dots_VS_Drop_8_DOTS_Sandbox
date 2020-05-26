using System;
using Unity.Entities;
using Unity.Transforms;

namespace Runtime
{
    [Serializable]
    public struct GetChildrenCount : IDataNode
    {
        [PortDescription(ValueType.Entity, "")]
        public InputDataPort GameObject;

        [PortDescription(ValueType.Int, "")]
        public OutputDataPort ChildrenCount;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity == Entity.Null)
                entity = ctx.CurrentEntity;

            var result = 0;

            if (ctx.EntityManager.HasComponent<Child>(entity))
            {
                var children = ctx.EntityManager.GetBuffer<Child>(entity);
                result = children.Length;
            }

            ctx.Write(ChildrenCount, result);
        }
    }
}
