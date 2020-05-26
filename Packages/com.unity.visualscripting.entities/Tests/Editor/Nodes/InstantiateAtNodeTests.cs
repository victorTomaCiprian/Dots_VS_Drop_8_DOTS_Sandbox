using System;
using System.Linq;
using NUnit.Framework;
using Runtime;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = Unity.Assertions.Assert;
using ValueType = Runtime.ValueType;

namespace Nodes
{
    public class InstantiateAtNodeTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void TestInstantiateAtNoEntity()
        {
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var instantiateAt = b.AddNode(new InstantiateAt());
                var log = b.AddNode(new Log { Messages = new InputDataMultiPort { DataCount = 1 }});

                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, instantiateAt.Input);
                b.CreateEdge(instantiateAt.Output, log.Input);
                b.CreateEdge(instantiateAt.Instantiated, log.Messages.SelectPort(0));
            },
                (manager, entity, index) => {},
                (manager, entity, index) => LogAssert.Expect(LogType.Log, ValueType.Unknown.ToString())
            );
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestInstantiateAtActivated(bool isActivated)
        {
            GraphBuilder.VariableHandle handle = default;

            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var instantiateAt = b.AddNode(new InstantiateAt());
                var constantActivatedValue = b.AddNode(new ConstantBool { Value = isActivated });

                b.BindVariableToInput(handle, instantiateAt.Prefab);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, instantiateAt.Input);
                b.CreateEdge(constantActivatedValue.ValuePort, instantiateAt.Activate);
            },
                (manager, entity, index) =>
                {
                    var instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);
                },
                (manager, entity, index) =>
                {
                    var e = manager.GetAllEntities().Last();
                    Assert.AreEqual(manager.GetEnabled(e), isActivated);
                });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestInstantiateAtPosition(bool hasComponent)
        {
            GraphBuilder.VariableHandle handle = default;
            var position = new float3(1f, 2f, 3f);

            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var instantiateAt = b.AddNode(new InstantiateAt());
                var constantPositionValue = b.AddNode(new ConstantFloat3 { Value = position });

                b.BindVariableToInput(handle, instantiateAt.Prefab);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, instantiateAt.Input);
                b.CreateEdge(constantPositionValue.ValuePort, instantiateAt.Position);
            },
                (manager, entity, index) =>
                {
                    var instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    if (hasComponent)
                        manager.AddComponent<Translation>(entity);
                },
                (manager, entity, index) =>
                {
                    var e = manager.GetAllEntities().Last();
                    Assert.IsTrue(manager.GetComponentData<Translation>(e).Value.Equals(position));
                });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestInstantiateAtRotation(bool hasComponent)
        {
            GraphBuilder.VariableHandle handle = default;
            var rotation = new quaternion(1f, 2f, 3f, 4f);

            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var instantiateAt = b.AddNode(new InstantiateAt());
                var constantPositionValue = b.AddNode(new ConstantQuaternion { Value = rotation });

                b.BindVariableToInput(handle, instantiateAt.Prefab);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, instantiateAt.Input);
                b.CreateEdge(constantPositionValue.ValuePort, instantiateAt.Rotation);
            },
                (manager, entity, index) =>
                {
                    var instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    if (hasComponent)
                        manager.AddComponent<Rotation>(entity);
                },
                (manager, entity, index) =>
                {
                    var e = manager.GetAllEntities().Last();
                    Assert.IsTrue(manager.GetComponentData<Rotation>(e).Value.Equals(rotation));
                });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestInstantiateAtScale(bool hasComponent)
        {
            GraphBuilder.VariableHandle handle = default;
            var scale = new float3(3f);

            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var instantiateAt = b.AddNode(new InstantiateAt());
                var scaleValue = b.AddNode(new ConstantFloat3 { Value = scale });

                b.BindVariableToInput(handle, instantiateAt.Prefab);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, instantiateAt.Input);
                b.CreateEdge(scaleValue.ValuePort, instantiateAt.Scale);
            },
                (manager, entity, index) =>
                {
                    var instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    if (hasComponent)
                    {
                        manager.AddComponent<Scale>(entity);
                        manager.AddComponent<NonUniformScale>(entity);
                    }
                },
                (manager, entity, index) =>
                {
                    var e = manager.GetAllEntities().Last();
                    Assert.IsTrue(manager.GetComponentData<Scale>(e).Value.Equals(scale.x));
                    Assert.IsFalse(manager.HasComponent<NonUniformScale>(e));
                });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestInstantiateAtNonUniformScale(bool hasComponent)
        {
            GraphBuilder.VariableHandle handle = default;
            var scale = new float3(1f, 2f, 4f);

            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var instantiateAt = b.AddNode(new InstantiateAt());
                var scaleValue = b.AddNode(new ConstantFloat3 { Value = scale });

                b.BindVariableToInput(handle, instantiateAt.Prefab);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, instantiateAt.Input);
                b.CreateEdge(scaleValue.ValuePort, instantiateAt.Scale);
            },
                (manager, entity, index) =>
                {
                    var instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    if (hasComponent)
                    {
                        manager.AddComponent<Scale>(entity);
                        manager.AddComponent<NonUniformScale>(entity);
                    }
                },
                (manager, entity, index) =>
                {
                    var e = manager.GetAllEntities().Last();
                    Assert.IsTrue(manager.GetComponentData<NonUniformScale>(e).Value.Equals(scale));
                    Assert.IsFalse(manager.HasComponent<Scale>(e));
                });
        }
    }
}
