using System;
using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Core;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class WaitUntilNodeTests : BaseFlowNodeStateRuntimeTests<WaitUntil, WaitUntil.State>
    {
        [Test]
        public void TestWaitUntilTrueCompletesImmediately()
        {
            m_GraphInstanceMock.Setup(x => x.ReadBool(m_Node.Condition)).Returns(true);
            TriggerPort(m_Node.Start);
            AssertPortTriggered(m_Node.OnDone, Times.Once());
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestWaitUntilCompletes()
        {
            bool isDone = false;
            m_GraphInstanceMock.Setup(x => x.ReadBool(m_Node.Condition)).Returns(() => isDone);
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never());
            isDone = true;
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Once());
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestWaitUntilMultipleStartTriggerOnce()
        {
            bool isDone = false;
            m_GraphInstanceMock.Setup(x => x.ReadBool(m_Node.Condition)).Returns(() => isDone);
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never());
            TriggerPort(m_Node.Start);
            UpdateNode();
            TriggerPort(m_Node.Start);
            UpdateNode();
            TriggerPort(m_Node.Start);
            isDone = true;
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Once());
            AssertNodeIsNotRunning();
        }
    }
}
