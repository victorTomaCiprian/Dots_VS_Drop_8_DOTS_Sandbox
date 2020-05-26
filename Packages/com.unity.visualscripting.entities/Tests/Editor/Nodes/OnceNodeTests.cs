using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class OnceNodeTests : BaseFlowNodeStateRuntimeTests<Once, Once.State>
    {
        [Test]
        public void TestRuntimeOnce()
        {
            TriggerPort(m_Node.Reset);
            AssertPortTriggered(m_Node.Output, Times.Never());
            Assert.IsFalse(m_GraphInstanceMock.Object.GetState(m_Node).Done);

            TriggerPort(m_Node.Input);
            AssertPortTriggered(m_Node.Output, Times.Once());
            Assert.IsTrue(m_GraphInstanceMock.Object.GetState(m_Node).Done);

            TriggerPort(m_Node.Input);
            AssertPortTriggered(m_Node.Output, Times.Once());
            Assert.IsTrue(m_GraphInstanceMock.Object.GetState(m_Node).Done);

            // Reset
            TriggerPort(m_Node.Reset);
            AssertPortTriggered(m_Node.Output, Times.Once());
            Assert.IsFalse(m_GraphInstanceMock.Object.GetState(m_Node).Done);

            TriggerPort(m_Node.Input);
            AssertPortTriggered(m_Node.Output, Times.Exactly(2));
            Assert.IsTrue(m_GraphInstanceMock.Object.GetState(m_Node).Done);
        }
    }
}
