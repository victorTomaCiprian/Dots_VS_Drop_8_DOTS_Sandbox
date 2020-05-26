using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Mathematics;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class StepperNodeTests : BaseFlowNodeStateRuntimeTests<Stepper, Stepper.State>
    {
        [Test]
        public void TestLoopStepper3()
        {
            m_Node.Mode = Stepper.OrderMode.Loop;
            m_Node.Step.SetCount(3);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 2, 0, 1, 2, 0);
        }

        [Test]
        public void TestLoopStepper5()
        {
            m_Node.Mode = Stepper.OrderMode.Loop;
            m_Node.Step.SetCount(5);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0);
        }

        [Test]
        public void TestStepperReset()
        {
            m_Node.Mode = Stepper.OrderMode.Loop;
            m_Node.Step.SetCount(3);
            CustomSetup();
            var calls = AssertTriggeredInOrder(0, 1);
            TriggerPort(m_Node.Reset);
            AssertTriggeredInOrder(calls, 0, 1, 2, 0);
            TriggerPort(m_Node.Reset);
            AssertTriggeredInOrder(calls, 0, 1, 2, 0);
        }

        [Test]
        public void TestHoldStepper2()
        {
            m_Node.Mode = Stepper.OrderMode.Hold;
            m_Node.Step.SetCount(2);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 1, 1, 1);
        }

        [Test]
        public void TestHoldStepper3()
        {
            m_Node.Mode = Stepper.OrderMode.Hold;
            m_Node.Step.SetCount(3);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 2, 2, 2, 2, 2, 2);
        }

        [Test]
        public void TestPingPongStepper2()
        {
            m_Node.Mode = Stepper.OrderMode.PingPong;
            m_Node.Step.SetCount(2);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 0, 1, 0);
        }

        [Test]
        public void TestPingPongStepper3()
        {
            m_Node.Mode = Stepper.OrderMode.PingPong;
            m_Node.Step.SetCount(3);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 2, 1, 0, 1, 2, 1, 0, 1, 2, 1);
        }

        [Test]
        public void TestPingPongStepper4()
        {
            m_Node.Mode = Stepper.OrderMode.PingPong;
            m_Node.Step.SetCount(4);
            CustomSetup();
            AssertTriggeredInOrder(0, 1, 2, 3, 2, 1, 0, 1, 2, 3, 2, 1, 0, 1);
        }

        int[] AssertTriggeredInOrder(params int[] order)
        {
            var callsCount = new int[m_Node.Step.DataCount];
            return AssertTriggeredInOrder(callsCount, order);
        }

        int[] AssertTriggeredInOrder(int[] callsCount, params int[] order)
        {
            foreach (var i in order)
            {
                TriggerPort(m_Node.In);
                callsCount[i]++; // only this trigger should fire
                // check that we fired the expected number of times on each port
                for (int j = 0; j < m_Node.Step.DataCount; j++)
                {
                    AssertPortTriggered(m_Node.Step.SelectPort((uint)j), Times.Exactly(callsCount[j]));
                }
            }

            return callsCount;
        }
    }
}
