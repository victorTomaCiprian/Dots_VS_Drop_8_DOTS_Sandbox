using System;

namespace Runtime
{
    [Serializable]
    public struct SetVariable : IFlowNode
    {
        public InputTriggerPort Input;
        public OutputTriggerPort Output;
        public InputDataPort Value;
        public OutputDataPort OutValue;
        public ValueType VariableType;
        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            var readValue = ctx.ReadValueOfType(Value, VariableType);
            ctx.Write(OutValue, readValue);
            ctx.Trigger(Output);
        }
    }
}
