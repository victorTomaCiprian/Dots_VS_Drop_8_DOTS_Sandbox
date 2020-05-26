using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class GetParentNodeTests : BaseDataNodeRuntimeTests<GetParent>
    {
        [Test]
        public void TestGetParentWithoutComponent()
        {
            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(m_World.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(Entity.Null);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Parent, Entity.Null), Times.Once());
        }

        [Test]
        public void TestGetParent()
        {
            var parent = m_World.EntityManager.CreateEntity();
            var child = m_World.EntityManager.CreateEntity();
            m_World.EntityManager.AddComponentData(child, new Parent { Value = parent });

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(m_World.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(child);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Parent, parent), Times.Once());
        }

        [Test]
        public void TestGetParentNotConnected()
        {
            var world = new World("GetParentNodeTests");
            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(Entity.Null);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Parent, Entity.Null), Times.Once());
        }
    }
}
