using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathComparisonBinaryTests : BaseDataNodeRuntimeTests<ComparisonBinary>
    {
        [TestCase(ComparisonBinary.ComparisonBinaryType.Equals, 1, 2, false)]
        [TestCase(ComparisonBinary.ComparisonBinaryType.Equals, 2, 2, true)]
        [TestCase(ComparisonBinary.ComparisonBinaryType.NotEquals, 2, 2, false)]
        [TestCase(ComparisonBinary.ComparisonBinaryType.NotEquals, 1, 2, true)]

        public void TestMathComparisonBinary(ComparisonBinary.ComparisonBinaryType type, float A, float B, bool Result)
        {
            m_Node.Type = type;
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.A)).Returns(A);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.B)).Returns(B);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, Result), Times.Once());
        }
    }
}
