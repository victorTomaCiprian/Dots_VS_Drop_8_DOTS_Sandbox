using System;
using Graphs;
using NUnit.Framework;
using Runtime;
using Unity.Entities;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nodes
{
    public class GetSetComponentTests : GraphRuntimeTests
    {
        public struct TestComponent : IComponentData
        {
            public int Int;
            public float Float;
        }

        [Test]
        public void TestGetComponent()
        {
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                var componentTypeRef = new TypeReference { TypeHash = TypeHash.CalculateStableTypeHash(typeof(TestComponent)) };
                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var getComponent = b.AddNode(new GetComponent { Type = componentTypeRef, ComponentData = new OutputDataMultiPort { DataCount = 2 }});
                var log = b.AddNode(new Log() { Messages = new InputDataMultiPort { DataCount = 1 }});

                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.CreateEdge(getComponent.ComponentData.SelectPort(0), log.Messages.SelectPort(0));
                b.AddReferencedComponent(componentTypeRef);
            }, (manager, entity, index) =>
                {
                    // don't add the component yet, next frame we can test that "get component" returns the default value
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "0"); // no component on entity should give default value
                    manager.AddComponentData(entity, new TestComponent { Int = 42, Float = 12.7f });
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                });
        }

        [Test]
        public void TestSetComponent()
        {
            GraphBuilder.VariableHandle varIndex = default;
            int initialValue = 42;
            int newValue = 55;
            SetupTestGraphDefinitionMultipleFrames((b, _) =>
            {
                var componentTypeRef = new TypeReference { TypeHash = TypeHash.CalculateStableTypeHash(typeof(TestComponent)) };
                var onUpdate = b.AddNode(new OnUpdate());
                var enabled = b.AddNode(new ConstantBool { Value = true });
                var setComponent = b.AddNode(new SetComponent { Type = componentTypeRef, ComponentData = new InputDataMultiPort { DataCount = 2 }});

                b.CreateEdge(enabled.ValuePort, onUpdate.Enabled);
                varIndex = b.BindVariableToDataIndex("a");
                b.BindVariableToInput(varIndex, setComponent.ComponentData.SelectPort(0));
                b.CreateEdge(onUpdate.Output, setComponent.Set);
                b.AddReferencedComponent(componentTypeRef);
            }
                , (manager, entity, index) =>
                {
                    // don't add the component yet, next frame we can test that "set component" doesn't do anything if no component is attached
                }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                    Assert.That(manager.HasComponent<TestComponent>(entity), Is.False); // Set component shouldn't add a component
                    manager.AddComponentData(entity, new TestComponent { Int = initialValue, Float = 12.7f });
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(varIndex.DataIndex, newValue);
                }, (manager, entity, index) =>
                {
                    var componentData = manager.GetComponentData<TestComponent>(entity);
                    Assert.That(componentData.Int, Is.EqualTo(newValue));
                });
        }
    }
}
