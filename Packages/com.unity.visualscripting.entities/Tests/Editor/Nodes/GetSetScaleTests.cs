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
    public class GetSetScaleTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void TestGetScale()
        {
            GraphBuilder.VariableHandle handle = default;
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var getScale = b.AddNode(new GetScale {});
                var log = b.AddNode(new Log() { Messages = new InputDataMultiPort { DataCount = 1 }});

                b.BindVariableToInput(handle, getScale.GameObject);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.CreateEdge(getScale.Value, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    manager.AddComponentData(entity, new NonUniformScale {Value = new float3(30, 2, 1)});
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "float3(30f, 2f, 1f)");
                });
        }

        [Test]
        public void TestSetScaleVector3()
        {
            GraphBuilder.VariableHandle handle = default;
            float3 setValue = new float3(42, 0, 0);
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var setScale = b.AddNode(new SetScale {});
                var constantRotationValue = b.AddNode(new ConstantFloat3()  { Value = setValue});

                b.BindVariableToInput(handle, setScale.GameObject);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, setScale.Input);
                b.CreateEdge(constantRotationValue.ValuePort, setScale.Value);
            }
                , (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    Assert.That(!manager.HasComponent<NonUniformScale>(entity));
                }, (manager, entity, index) =>
                {
                    NonUniformScale scale = manager.GetComponentData<NonUniformScale>(entity);
                    Assert.That(scale.Value.Equals(setValue));
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void TestSetScaleFloat()
        {
            GraphBuilder.VariableHandle handle = default;
            float setValue = 42.42f;
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                handle = b.BindVariableToDataIndex("TestEntity");

                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var setScale = b.AddNode(new SetScale {});
                var constantRotationValue = b.AddNode(new ConstantFloat()  { Value = setValue});

                b.BindVariableToInput(handle, setScale.GameObject);
                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, setScale.Input);
                b.CreateEdge(constantRotationValue.ValuePort, setScale.Value);
            }
                , (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(handle.DataIndex, entity);

                    Assert.That(!manager.HasComponent<NonUniformScale>(entity));
                }, (manager, entity, index) =>
                {
                    NonUniformScale scale = manager.GetComponentData<NonUniformScale>(entity);
                    Assert.That(scale.Value.Equals(setValue));
                    LogAssert.NoUnexpectedReceived();
                });
        }
    }
}
