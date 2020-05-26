using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class GetChildrenNodeTests : BaseDataNodeRuntimeTests<GetChildrenCount>
    {
        [Test]
        public void TestGetChildrenNoChildBuffer()
        {
            var world = new World("TestGetChildrenNoChildBuffer");
            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(Entity.Null);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildrenCount, 0), Times.Once());
        }

        [Test]
        public void TestGetChildren()
        {
            var world = new World("TestGetChildrenNoChildBuffer");
            var childA = world.EntityManager.CreateEntity();
            var childB = world.EntityManager.CreateEntity();
            var parent = world.EntityManager.CreateEntity();
            var children = world.EntityManager.AddBuffer<Child>(parent);
            children.Add(new Child { Value = childA });
            children.Add(new Child { Value = childB });

            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(world.EntityManager);
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.GameObject)).Returns(parent);
            m_Node.Execute(m_GraphInstanceMock.Object);
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.ChildrenCount, 2), Times.Once());
        }
    }
}
