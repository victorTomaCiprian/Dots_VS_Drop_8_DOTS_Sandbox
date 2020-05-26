using Moq;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;

namespace Nodes
{
    public class InstantiateNodeTests : BaseFlowNodeRuntimeTests<Instantiate>
    {
        [TestCase(true)]
        [TestCase(false)]
        public void TestRuntimeInstantiate(bool hasEntity)
        {
            m_GraphInstanceMock.Setup(x => x.EntityManager).Returns(m_World.EntityManager);

            var entity = hasEntity ? m_World.EntityManager.CreateEntity() : Entity.Null;
            m_GraphInstanceMock.Setup(x => x.ReadEntity(m_Node.Prefab)).Returns(entity);
            m_Node.Execute(m_GraphInstanceMock.Object, m_Node.Input);

            var instantiated = new Entity { Index = 1, Version = 1 };
            var times = hasEntity ? Times.Once() : Times.Never();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Instantiated, instantiated), times);
        }
    }
}
