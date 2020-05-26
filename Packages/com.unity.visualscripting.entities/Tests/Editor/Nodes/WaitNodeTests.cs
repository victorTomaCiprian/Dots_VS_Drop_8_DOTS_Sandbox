using System;
using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Core;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class WaitNodeTests : BaseFlowNodeStateRuntimeTests<Wait, Wait.State>
    {
        [Test]
        public void TestWaitZeroCompletesImmediately()
        {
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(0);
            TriggerPort(m_Node.Start);
            AssertPortTriggered(m_Node.OnDone, Times.Once());
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestWaitCompletes()
        {
            m_GraphInstanceMock.Object.Time = new TimeData(0.16, 0.1f);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(0.15f);
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Once()); // Internal time: 0.2
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestWaitStop()
        {
            m_GraphInstanceMock.Object.Time = new TimeData(0.16, 0.1f);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(0.15f);
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never());  // Internal time: 0
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never());  // Internal time: 0.1

            // Stop
            TriggerPort(m_Node.Stop);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never());  // Internal time: 0

            // Start
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Once()); // Internal time: 0.2
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestWaitPause()
        {
            m_GraphInstanceMock.Object.Time = new TimeData(0.16, 0.1f);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(0.15f);
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1

            // Pause
            TriggerPort(m_Node.Pause);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1

            // Start
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Once()); // Internal time: 0.2
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestWaitReset()
        {
            m_GraphInstanceMock.Object.Time = new TimeData(0.16, 0.1f);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(0.15f);
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1

            // Reset the timer
            TriggerPort(m_Node.Reset);
            AssertNodeIsRunning();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Never()); // Internal time: 0.1
            UpdateNode();
            AssertPortTriggered(m_Node.OnDone, Times.Once()); // Internal time: 0.2
            AssertNodeIsNotRunning();
        }
    }
}
