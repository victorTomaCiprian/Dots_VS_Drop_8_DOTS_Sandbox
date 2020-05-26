using System;
using Unity.Entities;
using Unity.Transforms;

namespace Runtime
{
    [Serializable]
    public struct EnumerateChildren : IFlowNode<EnumerateChildren.State>
    {
        [PortDescription("")]
        public InputTriggerPort NextChild;
        public InputTriggerPort Reset;

        [PortDescription("")]
        public OutputTriggerPort Out;
        public OutputTriggerPort Done;

        [PortDescription(ValueType.Entity, "")]
        public InputDataPort GameObject;

        [PortDescription(ValueType.Entity)]
        public OutputDataPort Child;

        [PortDescription(ValueType.Int, "Child Index")]
        public OutputDataPort ChildIndex;

        public struct State : INodeState
        {
            public int Index;
        }

        public Execution Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var go = ctx.ReadEntity(GameObject);
            if (go == Entity.Null)
                go = ctx.CurrentEntity;

            if (!ctx.EntityManager.HasComponent<Child>(go))
            {
                ctx.Write(Child, Entity.Null);
                ctx.Write(ChildIndex, -1);
                ctx.Trigger(Done);
                return Execution.Done;
            }

            ref State state = ref ctx.GetState(this);

            if (port == NextChild)
            {
                var children = ctx.EntityManager.GetBuffer<Child>(go);
                if (state.Index < children.Length)
                {
                    var child = children[state.Index];
                    ctx.Write(Child, child.Value);
                    ctx.Write(ChildIndex, state.Index);
                    ctx.Trigger(Out);
                    state.Index++;
                    return Execution.Done;
                }
            }

            ctx.Write(Child, Entity.Null);
            ctx.Write(ChildIndex, -1);

            if (port == Reset)
            {
                state.Index = 0;
                ctx.Trigger(Out);
                return Execution.Done;
            }

            ctx.Trigger(Done);
            return Execution.Done;
        }

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            throw new NotImplementedException();
        }
    }
}
