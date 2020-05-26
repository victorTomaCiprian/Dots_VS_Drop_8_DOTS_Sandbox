using System;
using Unity.Entities;
using UnityEditor.VisualScripting.Runtime;

namespace Runtime
{
    [Serializable]
    public struct HasComponent : IDataNode
    {
        [PortDescription(ValueType.Entity)]
        public InputDataPort Entity;

        [ComponentSearcher]
        public TypeReference Type;

        [PortDescription(ValueType.Bool)]
        public OutputDataPort Result;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var entity = ctx.ReadEntity(Entity);
            if (entity == Unity.Entities.Entity.Null)
                entity = ctx.CurrentEntity;

            ctx.Write(Result, ctx.EntityManager.HasComponent(entity, Type.GetComponentType()));
        }
    }
}
