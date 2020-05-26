using System;
using Graphs;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;
using VisualScripting.Model.Common.Extensions;

namespace Nodes
{
    public class GetSetRotationTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void TestGetRotation()
        {
            GraphBuilder.VariableHandle handle = default;
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var getRotation = b.AddNode(new GetRotation {});
                var log = b.AddNode(new Log() { Messages = new InputDataMultiPort { DataCount = 1 }});

                b.BindVariableToInput(handle, getRotation.GameObject);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.CreateEdge(getRotation.Value, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    manager.AddComponentData(entity, new Rotation {Value = Quaternion.Euler(30, 0, 0)});
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "quaternion(0.258819f, 0f, 0f, 0.9659258f)");
                });
        }

        [Test]
        public void TestSetRotationVector3()
        {
            GraphBuilder.VariableHandle handle = default;
            var setValue = new quaternion(0.258819f, 0f, 0f, 0.9659258f);
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var setRotation = b.AddNode(new SetRotation {});
                var constantRotationValue = b.AddNode(new ConstantQuaternion()  { Value = setValue});

                b.BindVariableToInput(handle, setRotation.GameObject);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, setRotation.Input);
                b.CreateEdge(constantRotationValue.ValuePort, setRotation.Value);
            }
                , (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    Assert.That(!manager.HasComponent<Rotation>(entity));
                }, (manager, entity, index) =>
                {
                    Rotation rotation = manager.GetComponentData<Rotation>(entity);
                    Quaternion q = rotation.Value;
                    Assert.That(q == setValue);
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void TestSetRotationEulerAngles()
        {
            GraphBuilder.VariableHandle handle = default;
            var setValue = new float3(-30f, 0f, 0f);
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var setRotation = b.AddNode(new SetRotation {});
                var quatFromEuler = b.AddNode(new RotationEuler());
                var constantRotationValue = b.AddNode(new ConstantFloat3()  { Value = setValue});

                b.BindVariableToInput(handle, setRotation.GameObject);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, setRotation.Input);
                b.CreateEdge(constantRotationValue.ValuePort, quatFromEuler.Euler);
                b.CreateEdge(quatFromEuler.Value, setRotation.Value);
            }
                , (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    Assert.That(!manager.HasComponent<Rotation>(entity));
                }, (manager, entity, index) =>
                {
                    Rotation rotation = manager.GetComponentData<Rotation>(entity);
                    var q = rotation.Value;
                    Assert.That(q, Is.EqualTo(quaternion.Euler(math.radians(setValue))));
                    LogAssert.NoUnexpectedReceived();
                });
        }
    }
}
