using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Core;
using Unity.Mathematics;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;

namespace Nodes
{
    public class TweenNodeTests : BaseFlowNodeStateRuntimeTests<Tween, Tween.State>
    {
        const float k_From = 15f;
        const float k_To = 20f;
        const float k_Duration = 0.15f;

        float m_Elapsed;

        public override void SetUp()
        {
            base.SetUp();
            m_Elapsed = 0;
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void TestTweenZeroDuration(float duration)
        {
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(duration);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.From)).Returns(15);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.To)).Returns(20);
            TriggerPort(m_Node.Start);
            AssertPortTriggered(m_Node.OnDone, Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Never());
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, 20), Times.Once());
            AssertNodeIsNotRunning();
        }

        [Test]
        public void TestTweenDone([Values] InterpolationType type)
        {
            SetupNode(type);

            // Internal time: 0
            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            // Internal time: 0.1
            UpdateNode();
            AssertNodeIsRunning();
            var result = ComputeResult(type);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            // Internal time: 0.2
            UpdateNode();
            AssertNodeIsNotRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_To), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Once());
        }

        [Test]
        public void TestTweenStopAndRestart([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Stop);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Exactly(2));
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        [Test]
        public void TestTweenStopAndPause([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Stop);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Pause);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        [Test]
        public void TestTweenStopAndReset([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Stop);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Reset);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        [Test]
        public void TestTweenPauseAndResume([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            UpdateNode();
            var result = ComputeResult(type);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());

            TriggerPort(m_Node.Pause);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Exactly(2));
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(3));
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        [Test]
        public void TestTweenPauseAndStop([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            UpdateNode();
            var result = ComputeResult(type);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());

            TriggerPort(m_Node.Pause);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Stop);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        [Test]
        public void TestTweenPauseAndReset([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            UpdateNode();
            var result = ComputeResult(type);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());

            TriggerPort(m_Node.Pause);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Reset);
            AssertNodeIsNotRunning();
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Exactly(2));
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(3));
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        [Test]
        public void TestTweenReset([Values] InterpolationType type)
        {
            SetupNode(type);

            TriggerPort(m_Node.Start);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Once());
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            UpdateNode();
            var result = ComputeResult(type);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(2));
            AssertPortTriggered(m_Node.OnDone, Times.Never());

            TriggerPort(m_Node.Reset);
            AssertNodeIsRunning();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, k_From), Times.Exactly(2));
            AssertPortTriggered(m_Node.OnFrame, Times.Exactly(3));
            AssertPortTriggered(m_Node.OnDone, Times.Never());
        }

        void SetupNode(InterpolationType type)
        {
            m_Node.Type = type;
            m_GraphInstanceMock.Object.Time = new TimeData(0.16, 0.1f);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.Duration)).Returns(k_Duration);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.From)).Returns(k_From);
            m_GraphInstanceMock.Setup(x => x.ReadFloat(m_Node.To)).Returns(k_To);
        }

        float ComputeResult(InterpolationType type)
        {
            m_Elapsed += m_GraphInstanceMock.Object.Time.DeltaTime;
            var progress = m_Elapsed / k_Duration;

            var result = type == InterpolationType.Linear
                ? math.lerp(k_From, k_To, progress)
                : Mathf.SmoothStep(k_From, k_To, progress);

            return result;
        }
    }
}
