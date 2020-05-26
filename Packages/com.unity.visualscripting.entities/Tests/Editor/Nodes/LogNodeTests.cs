using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Collections;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nodes
{
    public class LogNodeRuntimeTests : BaseFlowNodeRuntimeTests<Log>
    {
        [Test]
        public void TestRuntimeLogFloat()
        {
            m_Node.Messages.DataCount = 1;
            CustomSetup();
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(0))).Returns(2);
            TriggerPort(m_Node.Input);
            m_GraphInstanceMock.Verify(x => x.GetString(It.IsAny<StringReference>()), Times.Never());
            LogAssert.Expect(LogType.Log, "2");
            AssertPortTriggered(m_Node.Output, Times.Once());
        }

        [Test]
        public void TestRuntimeLogString()
        {
            m_Node.Messages.DataCount = 1;
            CustomSetup();
            // TODO Find what's missing in stringReference as this test just works when storing in a variable
            var stringRef = It.IsAny<StringReference>();

            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(0))).Returns(It.IsAny<StringReference>());
            TriggerPort(m_Node.Input);
            m_GraphInstanceMock.Verify(x => x.GetString(It.IsAny<StringReference>()), Times.Once());
            AssertPortTriggered(m_Node.Output, Times.Once());
        }

        [Test]
        public void TestRuntimeLogOneString()
        {
            m_Node.Messages.DataCount = 1;
            CustomSetup();
            var stringRef = new StringReference { Index = 42 };

            m_GraphInstanceMock.Setup(x => x.GetString(stringRef)).Returns(new NativeString128("42"));
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(0))).Returns(stringRef);
            TriggerPort(m_Node.Input);
            LogAssert.Expect(LogType.Log, "42");
            AssertPortTriggered(m_Node.Output, Times.Once());
        }

        [Test]
        public void TestRuntimeLogTwoStrings()
        {
            m_Node.Messages.DataCount = 2;
            CustomSetup();
            var stringRef4 = new StringReference { Index = 4 };
            var stringRef2 = new StringReference { Index = 2 };

            m_GraphInstanceMock.Setup(x => x.GetString(stringRef4)).Returns(new NativeString128("4"));
            m_GraphInstanceMock.Setup(x => x.GetString(stringRef2)).Returns(new NativeString128("2"));
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(0))).Returns(stringRef4);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(1))).Returns(stringRef2);
            TriggerPort(m_Node.Input);
            LogAssert.Expect(LogType.Log, "42");
            AssertPortTriggered(m_Node.Output, Times.Once());
        }

        [Test]
        public void TestLogStringsWithNonConnectedPorts()
        {
            m_Node.Messages.DataCount = 5;
            CustomSetup();
            var stringRef4 = new StringReference { Index = 4 };
            var stringRef2 = new StringReference { Index = 2 };

            m_GraphInstanceMock.Setup(x => x.GetString(stringRef4)).Returns(new NativeString128("4"));
            m_GraphInstanceMock.Setup(x => x.GetString(stringRef2)).Returns(new NativeString128("2"));
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(0))).Returns(new Value());
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(1))).Returns(stringRef4);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(2))).Returns(new Value());
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(3))).Returns(stringRef2);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Messages.SelectPort(4))).Returns(new Value());
            TriggerPort(m_Node.Input);
            LogAssert.Expect(LogType.Log, "Unknown4Unknown2Unknown");
            AssertPortTriggered(m_Node.Output, Times.Once());
        }
    }
}
