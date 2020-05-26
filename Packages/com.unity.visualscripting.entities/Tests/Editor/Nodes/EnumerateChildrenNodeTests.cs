using System;
using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class EnumerateChildrenNodeTests : BaseFlowNodeStateRuntimeTests<EnumerateChildren, EnumerateChildren.State>
    {
        [Test]
        public void TestEnumerateChildrenNoChildBuffer()
        {
            var world = new World("EnumerateChildrenNodeTests");

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(Entity.Null);
            TriggerPort(m_Node.NextChild);

            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, Entity.Null), Times.Once());
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildIndex, -1), Times.Once());
            AssertPortTriggered(m_Node.Out, Times.Never());
            AssertPortTriggered(m_Node.Done, Times.Once());
        }

        [Test]
        public void TestEnumerateChildrenReset()
        {
            var world = new World("EnumerateChildrenNodeTests");
            var parent = world.EntityManager.CreateEntity();
            world.EntityManager.AddBuffer<Child>(parent);

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(parent);
            TriggerPort(m_Node.Reset);

            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, Entity.Null), Times.Once());
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildIndex, -1), Times.Once());
            Assert.That(m_GraphInstanceMock.Object.GetState(m_Node).Index, Is.EqualTo(0));
            AssertPortTriggered(m_Node.Out, Times.Once());
            AssertPortTriggered(m_Node.Done, Times.Never());
        }

        [Test]
        public void TestEnumerateChildrenNextChild()
        {
            var world = new World("EnumerateChildrenNodeTests");
            var childA = world.EntityManager.CreateEntity();
            var childB = world.EntityManager.CreateEntity();
            var parent = world.EntityManager.CreateEntity();
            var children = world.EntityManager.AddBuffer<Child>(parent);
            children.Add(new Child { Value = childA });
            children.Add(new Child { Value = childB });

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(parent);

            Assert.That(m_GraphInstanceMock.Object.GetState(m_Node).Index, Is.EqualTo(0));
            TriggerPort(m_Node.NextChild);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, childA), Times.Once());
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildIndex, 0), Times.Once());

            Assert.That(m_GraphInstanceMock.Object.GetState(m_Node).Index, Is.EqualTo(1));
            TriggerPort(m_Node.NextChild);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, childB), Times.Once());
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildIndex, 1), Times.Once());

            Assert.That(m_GraphInstanceMock.Object.GetState(m_Node).Index, Is.EqualTo(2));
            TriggerPort(m_Node.NextChild);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, Entity.Null), Times.Once());
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildIndex, -1), Times.Once());

            AssertPortTriggered(m_Node.Out, Times.Exactly(2));
            AssertPortTriggered(m_Node.Done, Times.Once());
        }

        [Test]
        public void TestEnumerateChildrenNoUpdateCall()
        {
            Assert.Throws<NotImplementedException>(() => m_Node.Update(m_GraphInstanceMock.Object));
        }
    }
}
