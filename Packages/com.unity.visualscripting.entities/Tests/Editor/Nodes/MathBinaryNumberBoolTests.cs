using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathComparisonBinaryBoolTests : BaseDataNodeRuntimeTests<MathBinaryNumberBool>
    {
        [TestCase(MathBinaryNumberBool.BinaryNumberType.GreaterThan, 1, 2, false)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.GreaterThan, 1, 1, false)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.GreaterThan, 2, 1, true)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.GreaterThanOrEqual, 1, 2, false)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.GreaterThanOrEqual, 1, 1, true)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.GreaterThanOrEqual, 2, 1, true)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.LessThan, 1, 2, true)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.LessThan, 1, 1, false)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.LessThan, 2, 1, false)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.LessThanOrEqual, 1, 2, true)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.LessThanOrEqual, 1, 1, true)]
        [TestCase(MathBinaryNumberBool.BinaryNumberType.LessThanOrEqual, 2, 1, false)]

        public void TestMathComparisonBinaryBool(MathBinaryNumberBool.BinaryNumberType type, float A, float B, bool Result)
        {
            m_Node.Type = type;
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.A)).Returns(A);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.B)).Returns(B);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, Result), Times.Once());
        }
    }
}
