using System;
using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class StateSwitchNodeTests : BaseFlowNodeRuntimeTests<StateSwitch>
    {
        [Test]
        public void SetFalseWritesOutputValue()
        {
            m_Node.Execute(m_GraphInstanceMock.Object, m_Node.SetFalse);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.State, false), Times.Once());
            AssertPortTriggered(m_Node.Done, Times.Once());
        }

        [Test]
        public void SetTrueWritesOutputValue()
        {
            m_Node.Execute(m_GraphInstanceMock.Object, m_Node.SetFalse);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.State, false), Times.Once());
            AssertPortTriggered(m_Node.Done, Times.Once());
        }
    }
}
