using System;
using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class AllNodeTests : BaseFlowNodeStateRuntimeTests<WaitForAll, WaitForAll.State>
    {
        [Test]
        public void TestAllWorks()
        {
            m_Node.Input.SetCount(2);
            CustomSetup();

            TriggerPort(m_Node.Input.SelectPort(0));
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Input.SelectPort(0));
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Input.SelectPort(1));
            AssertPortTriggered(m_Node.Output, Times.Once());
        }

        [Test]
        public void TestAllWorks3Inputs()
        {
            m_Node.Input.SetCount(3);
            CustomSetup();

            TriggerPort(m_Node.Input.SelectPort(0));
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Input.SelectPort(1));
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Input.SelectPort(2));
            AssertPortTriggered(m_Node.Output, Times.Once());
        }

        [Test]
        public void TestRuntimeAllCanBeReset()
        {
            m_Node.Input.SetCount(2);
            CustomSetup();

            TriggerPort(m_Node.Input.SelectPort(0));
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Reset);
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Input.SelectPort(1));
            AssertPortTriggered(m_Node.Output, Times.Never());

            TriggerPort(m_Node.Input.SelectPort(0));
            AssertPortTriggered(m_Node.Output, Times.Once());
        }
    }
}
