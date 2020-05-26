using Moq;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathBinaryNodeTests : BaseDataNodeRuntimeTests<MathGenericNode>
    {
        [TestCase(MathGeneratedFunction.SubtractFloatFloat, -2)]
        [TestCase(MathGeneratedFunction.DivideFloatFloat, 0.5f)]
        [TestCase(MathGeneratedFunction.ModuloIntInt, 2)]
        [TestCase(MathGeneratedFunction.Atan2FloatFloat, 0.4636476f)]
        [TestCase(MathGeneratedFunction.PowFloatFloat, 16)]
        public void TestRuntimeBinaryNumber(MathGeneratedFunction function, float result)
        {
            m_Node = m_Node.WithFunction(function);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Inputs.SelectPort(0))).Returns(2);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Inputs.SelectPort(1))).Returns(4);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
        }
    }
}
