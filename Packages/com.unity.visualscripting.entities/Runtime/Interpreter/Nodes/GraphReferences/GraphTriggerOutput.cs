using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.Assertions;

namespace Runtime
{
    public struct GraphTriggerOutput : IFlowNode<GraphTriggerOutput.EmptyState>
    {
        public struct EmptyState : INodeState {}

        public InputTriggerPort Input;
        public uint OutputIndex;

        public Execution Update<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            throw new NotImplementedException();
        }

        Execution IStateFlowNode.Execute<TCtx>(TCtx ctx, InputTriggerPort port)
        {
            return ctx.TriggerGraphOutput(OutputIndex);
        }
    }
    public struct GraphDataInput : IDataNode
    {
        public OutputDataPort Output;
        // First one will have the id 1, 0 is considered invalid
        public uint InputDataId;
        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            Assert.AreNotEqual(0u, InputDataId);
            Value value = ctx.ReadGraphInputValue((int)(InputDataId - 1));
            ctx.Write(Output, value);
        }
    }
}
