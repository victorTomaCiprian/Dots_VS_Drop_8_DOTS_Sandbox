using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class GetChildAtNodeTests : BaseDataNodeRuntimeTests<GetChildAt>
    {
        [Test]
        public void TestGetChildAtNoChildBuffer()
        {
            var world = new World("GetChildAtNodeTests");
            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(Entity.Null);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, Entity.Null), Times.Once());
        }

        [Test]
        public void TestGetChildAt()
        {
            var world = new World("GetChildAtNodeTests");
            var childA = world.EntityManager.CreateEntity();
            var childB = world.EntityManager.CreateEntity();
            var parent = world.EntityManager.CreateEntity();
            var children = world.EntityManager.AddBuffer<Child>(parent);
            children.Add(new Child { Value = childA });
            children.Add(new Child { Value = childB });

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(parent);
            m_GraphInstanceMock.Setup(x => x.ReadInt(m_Node.Index)).Returns(1);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, childB), Times.Once());
        }

        [Test]
        public void TestGetChildAtIndexOutOfBounds()
        {
            var world = new World("GetChildAtNodeTests");
            var childA = world.EntityManager.CreateEntity();
            var childB = world.EntityManager.CreateEntity();
            var parent = world.EntityManager.CreateEntity();
            var children = world.EntityManager.AddBuffer<Child>(parent);
            children.Add(new Child { Value = childA });
            children.Add(new Child { Value = childB });

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(parent);
            m_GraphInstanceMock.Setup(x => x.ReadInt(m_Node.Index)).Returns(3);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Child, Entity.Null), Times.Once());
        }
    }
}
