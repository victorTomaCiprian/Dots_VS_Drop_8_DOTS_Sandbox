using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Runtime
{
    [Serializable]
    public struct Log : IFlowNode
    {
        [PortDescription("")]
        public InputTriggerPort Input;
        [PortDescription("")]
        public OutputTriggerPort Output;
        public InputDataMultiPort Messages;

        public void Execute<TCtx>(TCtx ctx, InputTriggerPort port) where TCtx : IGraphInstance
        {
            Assert.AreEqual(Input.Port.Index, port.Port.Index);
            string message = null;

            for (uint i = 0; i < Messages.DataCount; i++)
            {
                ConcatToMessage(ctx, ctx.ReadValue(Messages.SelectPort(i)), ref message);
            }

            if (message != null)
                Debug.Log(message);

            ctx.Trigger(Output);
        }

        static void ConcatToMessage<TCtx>(TCtx ctx, Value value, ref string message) where TCtx : IGraphInstance
        {
            if (message == null)
                message = "";
            switch (value.Type)
            {
                case ValueType.StringReference:
                    message += ctx.GetString(value.StringReference).ToString();
                    break;
                case ValueType.Entity:
                    message +=  ctx.GetString(value.Entity).ToString();
                    break;
                default:
                    message += value.ToString();
                    break;
            }
        }
    }
}
