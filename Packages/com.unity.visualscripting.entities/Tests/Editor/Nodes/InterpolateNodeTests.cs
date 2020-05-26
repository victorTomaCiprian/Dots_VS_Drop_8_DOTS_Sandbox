using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class InterpolateNodeTests : BaseDataNodeRuntimeTests<Interpolate>
    {
        [TestCase(InterpolationType.Linear, -1, 10)]
        [TestCase(InterpolationType.SmoothStep, -1, 10)]
        [TestCase(InterpolationType.Linear, 0, 10)]
        [TestCase(InterpolationType.SmoothStep, 0, 10)]
        [TestCase(InterpolationType.Linear, 0.5f, 15)]
        [TestCase(InterpolationType.SmoothStep, 0.5f, 15)]
        [TestCase(InterpolationType.Linear, 1, 20)]
        [TestCase(InterpolationType.SmoothStep, 1, 20)]
        [TestCase(InterpolationType.Linear, 2, 20)]
        [TestCase(InterpolationType.SmoothStep, 2, 20)]
        public void TestInterpolate(InterpolationType type, float progress, float result)
        {
            m_Node.Type = type;
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.From)).Returns(10);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.To)).Returns(20);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Progress)).Returns(progress);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
        }
    }
}
