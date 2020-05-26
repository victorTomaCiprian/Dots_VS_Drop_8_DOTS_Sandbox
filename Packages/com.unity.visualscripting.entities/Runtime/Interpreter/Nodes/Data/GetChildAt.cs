using System;
using Unity.Entities;
using Unity.Transforms;

namespace Runtime
{
    [Serializable]
    public struct GetChildAt : IDataNode
    {
        [PortDescription(ValueType.Entity, "")]
        public InputDataPort GameObject;

        [PortDescription(ValueType.Int)]
        public InputDataPort Index;

        [PortDescription(ValueType.Entity, "")]
        public OutputDataPort Child;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(GameObject);
            if (entity == Entity.Null)
                entity = ctx.CurrentEntity;

            var result = Entity.Null;
            var index = ctx.ReadInt(Index);

            if (ctx.EntityManager.HasComponent<Child>(entity))
            {
                var children = ctx.EntityManager.GetBuffer<Child>(entity);
                if (index < children.Length)
                {
                    var child = children[index];
                    result = child.Value;
                }
            }

            ctx.Write(Child, result);
        }
    }
}
