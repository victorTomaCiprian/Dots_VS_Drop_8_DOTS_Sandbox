using System;
using System.Collections.Generic;
using System.Linq;
using DotsStencil;
using Runtime;
using Runtime.Mathematics;
using Unity.Mathematics;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace NodeModels
{
    [Serializable, GeneratedMathSearcherAttribute("Math")]
    class MathNodeModel : DotsNodeModel<MathGenericNode>, IHasMainOutputPort
    {
        public override string Title => m_MethodName;

        [HackContextualMenuVariableCount("Input")]
        public int InputCount;

        public IPortModel OutputPort { get; set; }

        string m_MethodName;
        MathOperationsMetaData.OpSignature m_CurrentMethod;
        MathOperationsMetaData.OpSignature[] m_CompatibleMethods;

        public override IReadOnlyDictionary<string, List<PortMetaData>> PortCustomData => new Dictionary<string, List<PortMetaData>>
        {
            {nameof(MathGenericNode.Inputs), new List<PortMetaData>(InputPortsMetadata())},
            {nameof(MathGenericNode.Result), new List<PortMetaData> { ResultPortsMetadata() } },
        };

        public override IReadOnlyDictionary<string, PortCountProperties> PortCountData => new Dictionary<string, PortCountProperties>
        {
            {nameof(InputCount), InputPortCountData()},
        };

        PortCountProperties InputPortCountData()
        {
            var portCount = TypedNode.Inputs.DataCount;
            var result = new PortCountProperties { Min = portCount, Max = portCount, Name = "Input" };
            var methodName = TypedNode.Function.GetMethodsSignature().OpType;
            if (MathOperationsMetaData.MethodNameSupportsMultipleInputs(methodName))
            {
                result.Max = -1;
                result.Min = 2;
            }
            return result;
        }

        PortMetaData ResultPortsMetadata()
        {
            var returnData = GetPortMetadata(nameof(MathGenericNode.Result));
            returnData.Type = m_CurrentMethod.Return;
            return returnData;
        }

        IEnumerable<PortMetaData> InputPortsMetadata()
        {
            var defaultData = GetPortMetadata(nameof(MathGenericNode.Inputs));
            for (int i = 0; i < InputCount; i++)
            {
                defaultData.Name = "";
                defaultData.Type = m_CurrentMethod.Params[math.min(i, m_CurrentMethod.Params.Length - 1)];
                defaultData.DefaultValue = Activator.CreateInstance(defaultData.Type.ValueTypeToTypeHandle().Resolve(Stencil));
                yield return defaultData;
            }
        }

        protected override void OnDefineNode()
        {
            m_MethodName = TypedNode.Type.GetMethodsSignature().OpType;
            m_CompatibleMethods = MathOperationsMetaData.MethodsByName[m_MethodName];
            m_CurrentMethod = m_CompatibleMethods.Single(o => o.EnumName == TypedNode.Type.ToString());
            if (InputCount == default)
                InputCount = m_CurrentMethod.Params.Length;

            var mathGenericNode = TypedNode;
            mathGenericNode.GenerationVersion = MathGeneratedDelegates.GenerationVersion;
            Node = mathGenericNode;

            base.OnDefineNode();
        }
    }
}
