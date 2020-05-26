using System;
using System.Linq;
using Runtime;
using Runtime.Mathematics;
using NodeModels;

namespace UnityEditor.VisualScriptingECSTests
{
    static class GraphBuilderExtensions
    {
        public static OnUpdate AddUpdate(this GraphBuilder b, bool enabled = true)
        {
            var constBool = b.AddNode(new ConstantBool { Value = true });
            var onUpdate = b.AddNode(new OnUpdate());
            b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
            return onUpdate;
        }

        public static Log AddLog(this GraphBuilder b, int numMessages = 1)
        {
            return b.AddNode(new Log { Messages = new InputDataMultiPort { DataCount = numMessages } });
        }

        public static MathGenericNode AddMath(this GraphBuilder b, MathGeneratedFunction function)
        {
            return b.AddNode(new MathGenericNode().WithFunction(function));
        }

        public static MathGenericNode WithFunction(this MathGenericNode node, MathGeneratedFunction function)
        {
            var methodName = function.GetMethodsSignature().OpType;
            var compatibleMethods = MathOperationsMetaData.MethodsByName[methodName];
            var currentMethod = compatibleMethods.Single(o => o.EnumName == function.ToString());
            node.Function = function;
            node.Inputs.DataCount = currentMethod.Params.Length;
            node.GenerationVersion = MathGeneratedDelegates.GenerationVersion;
            return node;
        }

        public static BindingId ToBidingId(this string s) => BindingId.From((ulong)s.GetHashCode(), 0);

        public static GraphBuilder.VariableHandle BindVariableToDataIndex(this GraphBuilder b, string variableName)
        {
            return b.BindVariableToDataIndex(variableName.ToBidingId());
        }
    }
}
