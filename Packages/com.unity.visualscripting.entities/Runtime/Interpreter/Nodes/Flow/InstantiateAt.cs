using System;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct InstantiateAt : IFlowNode
    {
        [UsedImplicitly, PortDescription("")]
        public InputTriggerPort Input;

        [PortDescription(ValueType.Entity, "")]
        public InputDataPort Prefab;

        [PortDescription(ValueType.Bool, DefaultValue = true)]
        public InputDataPort Activate;

        [PortDescription(ValueType.Float3)]
        public InputDataPort Position;

        [PortDescription(ValueType.Quaternion)]
        public InputDataPort Rotation;

        [PortDescription(ValueType.Float3)]
        public InputDataPort Scale;

        [PortDescription("")]
        public OutputTriggerPort Output;

        [PortDescription(ValueType.Entity, "")]
        public OutputDataPort Instantiated;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var prefab = ctx.ReadEntity(Prefab);
            if (prefab == Entity.Null)
            {
                ctx.Trigger(Output);
                return;
            }

            var entity = ctx.EntityManager.Instantiate(prefab);

            var activated = ctx.ReadBool(Activate);
            ctx.EntityManager.SetEnabled(entity, activated);

            AddComponent(ctx, entity, new Translation { Value = ctx.ReadFloat3(Position) });
            AddComponent(ctx, entity, new Rotation { Value = ctx.ReadQuaternion(Rotation) });

            var scale = ctx.ReadFloat3(Scale);
            var isUniformScale = scale.x.Equals(scale.y) && scale.x.Equals(scale.z);
            if (isUniformScale)
            {
                if (ctx.EntityManager.HasComponent<NonUniformScale>(entity))
                    ctx.EntityManager.RemoveComponent<NonUniformScale>(entity);
                AddComponent(ctx, entity, new Scale { Value = scale.x });
            }
            else
            {
                if (ctx.EntityManager.HasComponent<Scale>(entity))
                    ctx.EntityManager.RemoveComponent<Scale>(entity);
                AddComponent(ctx, entity, new NonUniformScale { Value = scale });
            }

            ctx.Write(Instantiated, entity);
            ctx.Trigger(Output);
        }

        void AddComponent<T>(IGraphInstance ctx, Entity entity, T componentData) where T : struct, IComponentData
        {
            if (!ctx.EntityManager.HasComponent<T>(entity))
                ctx.EntityManager.AddComponent<T>(entity);
            ctx.EntityManager.SetComponentData(entity, componentData);
        }
    }
}
