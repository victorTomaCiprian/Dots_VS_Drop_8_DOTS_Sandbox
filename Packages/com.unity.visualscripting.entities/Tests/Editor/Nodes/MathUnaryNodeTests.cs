using Moq;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using Unity.Mathematics;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathUnaryNodeTests : BaseDataNodeRuntimeTests<MathGenericNode>
    {
        [TestCase(MathGeneratedFunction.NegateFloat, 2, -2)]
        [TestCase(MathGeneratedFunction.SinFloat, math.PI / 2, 1)]
        [TestCase(MathGeneratedFunction.CosFloat, math.PI, -1)]
        [TestCase(MathGeneratedFunction.TanFloat, math.PI / 4, 1)]
        [TestCase(MathGeneratedFunction.RoundFloat, 2.5f, 2)]
        [TestCase(MathGeneratedFunction.RoundFloat, 3.5f, 4)]
        [TestCase(MathGeneratedFunction.CeilFloat, 2.1f, 3)]
        [TestCase(MathGeneratedFunction.FloorFloat, 2.9f, 2)]
        [TestCase(MathGeneratedFunction.AbsFloat, -5f, 5)]
        [TestCase(MathGeneratedFunction.SinhFloat, 0f, 0)]
        [TestCase(MathGeneratedFunction.CoshFloat, 0f, 1)]
        [TestCase(MathGeneratedFunction.TanhFloat, 0f, 0)]
        [TestCase(MathGeneratedFunction.AsinFloat, 1f, math.PI / 2)]
        [TestCase(MathGeneratedFunction.AcosFloat, -1f, math.PI)]
        [TestCase(MathGeneratedFunction.AtanFloat, 1f, math.PI / 4)]
        [TestCase(MathGeneratedFunction.ExpFloat, 4f, 54.59815f)]
        [TestCase(MathGeneratedFunction.Log10Float, 100f, 2)]
        [TestCase(MathGeneratedFunction.Log2Float, 16f, 4)]
        [TestCase(MathGeneratedFunction.SignFloat, -2f, -1)]
        [TestCase(MathGeneratedFunction.SqrtFloat, 16f, 4)]
        [TestCase(MathGeneratedFunction.CubicRootFloat, 27f, 3)]

        public void TestRuntimeMathUnary(MathGeneratedFunction function, float value, float result)
        {
            m_Node = m_Node.WithFunction(function);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Inputs.SelectPort(0))).Returns(value);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
        }
    }

    public class MathUnaryNotBoolTests : BaseDataNodeRuntimeTests<MathUnaryNotBool>
    {
        [TestCase(true, false)]
        [TestCase(false, true)]

        public void TestMathUnaryNotBoolTests(bool input, bool output)
        {
            m_GraphInstanceMock.Setup(x => x.ReadBool(m_Node.Value)).Returns(input);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, output), Times.Once());
        }
    }
}
