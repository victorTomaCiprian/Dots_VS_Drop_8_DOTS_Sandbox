using System;
using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class IfNodeTests : BaseFlowNodeRuntimeTests<If>
    {
        [TestCase(false)]
        [TestCase(true)]
        public void TestIf(bool cond)
        {
            CustomSetup();
            m_GraphInstanceMock.Setup(x => x.ReadBool(m_Node.Condition)).Returns(cond);
            TriggerPort(m_Node.Input);
            if (cond)
            {
                AssertPortTriggered(m_Node.IfTrue, Times.Once());
                AssertPortTriggered(m_Node.IfFalse, Times.Never());
            }
            else
            {
                AssertPortTriggered(m_Node.IfFalse, Times.Once());
                AssertPortTriggered(m_Node.IfTrue, Times.Never());
            }
        }
    }
}
