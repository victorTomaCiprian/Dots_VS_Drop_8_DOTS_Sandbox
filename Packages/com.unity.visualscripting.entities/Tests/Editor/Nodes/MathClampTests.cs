using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathClampTests : BaseDataNodeRuntimeTests<Clamp>
    {
        [TestCase(1, 2)]
        [TestCase(10, 4)]
        public void TestMathClamp(float value, float result)
        {
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Value)).Returns(value);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Min)).Returns(2);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Max)).Returns(4);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
        }
    }
}
