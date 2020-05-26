using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class SwitchNodeTests : BaseFlowNodeRuntimeTests<Switch>
    {
        [Test]
        public void TestSwitch()
        {
            SetExpectedValue(42);

            InputSetup(0, 1, 42, 3);
            TriggerPort(m_Node.Input);
            var triggerCount = AssertOnePortTriggered(2);
            AssertPortTriggered(m_Node.Default, Times.Never());

            InputSetup(0, 1, 2, 42);
            TriggerPort(m_Node.Input);
            AssertOnePortTriggered(3, triggerCount);
            AssertPortTriggered(m_Node.Default, Times.Never());

            SetExpectedValue(0); // test default value

            InputSetup(1, 0, 2, 3);
            TriggerPort(m_Node.Input);
            AssertOnePortTriggered(1, triggerCount);
            AssertPortTriggered(m_Node.Default, Times.Never());
        }

        [Test]
        public void TestSwitchEmptyTriggersDefaultCase()
        {
            SetExpectedValue(42);

            InputSetup();
            TriggerPort(m_Node.Input);
            AssertPortTriggered(m_Node.Default, Times.Once());

            SetExpectedValue(0); // test default value

            InputSetup();
            TriggerPort(m_Node.Input);
            AssertPortTriggered(m_Node.Default, Times.Exactly(2));
        }

        [Test]
        public void TestSwitchMultiMatch()
        {
            SetExpectedValue(42);

            InputSetup(42, 1, 42, 42, 2);
            TriggerPort(m_Node.Input);
            var triggerCount = AssertPortsTriggered(true, false, true, true, false);
            AssertPortTriggered(m_Node.Default, Times.Never());

            InputSetup(1, 42, 42, 42, 2);
            TriggerPort(m_Node.Input);
            AssertPortsTriggered(triggerCount, false, true, true, true, false);
            AssertPortTriggered(m_Node.Default, Times.Never());

            SetExpectedValue(0); // test default value

            InputSetup(0, 1, 0, 1, 0);
            TriggerPort(m_Node.Input);
            AssertPortsTriggered(triggerCount, true, false, true, false, true);
            AssertPortTriggered(m_Node.Default, Times.Never());
        }

        [Test]
        public void TestSwitchNoMatch()
        {
            SetExpectedValue(42);

            InputSetup(0, 1, 2, 3, 4);
            TriggerPort(m_Node.Input);
            AssertNoPortTriggered();
            AssertPortTriggered(m_Node.Default, Times.Once());

            SetExpectedValue(0); // test default value

            InputSetup(1, 2, 3, 4, 5);
            TriggerPort(m_Node.Input);
            AssertNoPortTriggered();
            AssertPortTriggered(m_Node.Default, Times.Exactly(2));
        }

        void SetCount(int count)
        {
            m_Node.SwitchValues.SetCount(count);
            m_Node.SwitchTriggers.SetCount(count);
            CustomSetup();
        }

        void InputSetup(params int[] values)
        {
            SetCount(values.Length);
            for (uint i = 0; i < m_Node.SwitchValues.DataCount; i++)
            {
                var port = m_Node.SwitchValues.SelectPort(i);
                m_GraphInstanceMock.Setup(x => x.ReadInt(port)).Returns(values[i]);
            }
        }

        void SetExpectedValue(int value)
        {
            m_GraphInstanceMock.Setup(x => x.ReadInt(m_Node.Selector)).Returns(value);
        }

        int[] AssertNoPortTriggered(int[] previousTriggerCount = null)
        {
            return AssertPortsTriggered(new bool[m_Node.SwitchValues.DataCount]);
        }

        int[] AssertOnePortTriggered(uint portIndex, int[] previousTriggerCount = null)
        {
            bool[] portsToTrigger = new bool[m_Node.SwitchValues.DataCount];
            portsToTrigger[portIndex] = true;
            return AssertPortsTriggered(previousTriggerCount ?? new int[portsToTrigger.Length], portsToTrigger);
        }

        int[] AssertPortsTriggered(params bool[] indicesToTrigger)
        {
            return AssertPortsTriggered(new int[indicesToTrigger.Length], indicesToTrigger);
        }

        int[] AssertPortsTriggered(int[] previousTriggerCount, params bool[] indicesToTrigger)
        {
            for (uint i = 0; i < previousTriggerCount.Length; i++)
            {
                if (indicesToTrigger[i])
                    previousTriggerCount[i]++;
                AssertPortTriggered(m_Node.SwitchTriggers.SelectPort(i), Times.Exactly(previousTriggerCount[i]));
            }

            return previousTriggerCount;
        }
    }
}
