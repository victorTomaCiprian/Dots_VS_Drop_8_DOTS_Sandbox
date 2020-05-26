using NUnit.Framework;
using Runtime;
using Unity.Entities;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nodes
{
    public class SendOnEventTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => false;
        static ulong TestEventId => 0xDEADBEEF; // actually doesn't really matter in those tests as runtime only passes around the ulong hash

        [Test]
        public void TestSendEventToSelf()
        {
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default;
            SetupTestGraphDefinitionMultipleFrames(b =>
            {
                AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                AddOnEventLogIndices(b, TestEventId, 0, 1);
            }, (manager, entity, index) =>
                {
                    Assert.That(entity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42-4200");
                });
        }

        [Test]
        public void TestSendEventToOtherGraph()
        {
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default;
            Entity senderEntity = default;
            SetupMultiEntitiesTestGraph(new EntityTestSetup(
                e => senderEntity = e,
                b =>
                {
                    AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                }), new EntityTestSetup(b =>
                {
                    AddOnEventLogIndices(b, TestEventId, 0, 1);
                }), (manager, entities, index) =>
                {
                    Assert.That(senderEntity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(senderEntity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42-4200");
                });
        }

        [Test]
        public void TestReceiveUnsentEvent()
        {
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default;
            Entity senderEntity = default;
            SetupMultiEntitiesTestGraph(new EntityTestSetup(
                e => senderEntity = e,
                b =>
                {
                    AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                }), new EntityTestSetup(b =>
                {
                    AddOnEventLogIndices(b, 0xBAD, 0, 1);
                }), (manager, entities, index) =>
                {
                    Assert.That(senderEntity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(senderEntity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void TestSendEventMultipleReceivers()
        {
            Entity senderEntity = default;
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default;
            SetupMultiEntitiesTestGraph(
                new EntityTestSetup(e => senderEntity = e,
                    b =>
                    {
                        AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                    }),
                new EntityTestSetup(b =>
                {
                    AddOnEventLogIndex(b, TestEventId, 0);
                }),
                new EntityTestSetup(
                    b =>
                    {
                        AddOnEventLogIndex(b, TestEventId, 1);
                    }),
                (manager, entities, index) =>
                {
                    Assert.That(senderEntity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(senderEntity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                },
                (manager, entities, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                    LogAssert.Expect(LogType.Log, "-4200");
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void TestSendEventToSelfAndOther()
        {
            Entity senderEntity = default;
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default;
            SetupMultiEntitiesTestGraph(
                new EntityTestSetup(e => senderEntity = e,
                    b =>
                    {
                        AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                        AddOnEventLogIndex(b, TestEventId, 0);
                    }),
                new EntityTestSetup(b =>
                {
                    AddOnEventLogIndex(b, TestEventId, 1);
                }),
                (manager, entities, index) =>
                {
                    Assert.That(senderEntity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(senderEntity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                },
                (manager, entities, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                    LogAssert.Expect(LogType.Log, "-4200");
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void TestTargetedSendEvent()
        {
            Entity senderEntity = default;
            Entity targetEntity = default;
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default, varEntityIndex = default;
            SetupMultiEntitiesTestGraph(
                new EntityTestSetup(e => senderEntity = e,
                    b =>
                    {
                        var send = AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                        varEntityIndex = b.BindVariableToDataIndex("entity");
                        b.BindVariableToInput(varEntityIndex, send.Entity);
                    }),
                new EntityTestSetup(e => targetEntity = e,
                    b => { AddOnEventLogIndex(b, TestEventId, 0); }
                ),
                new EntityTestSetup(
                    b => { AddOnEventLogIndex(b, TestEventId, 1); }
                ),
                (manager, entities, index) =>
                {
                    Assert.That(senderEntity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(senderEntity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                    Assert.That(targetEntity, Is.Not.EqualTo(Entity.Null));
                    instance.WriteValueToDataSlot(varEntityIndex.DataIndex, targetEntity);
                },
                (manager, entities, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void TestEventNotSentWhenConnectedToNullEntity()
        {
            Entity senderEntity = default;
            Entity targetEntity = default;
            GraphBuilder.VariableHandle varAIndex = default, varBIndex = default, varEntityIndex = default;
            SetupMultiEntitiesTestGraph(
                new EntityTestSetup(e => senderEntity = e,
                    b =>
                    {
                        var send = AddEventSender(b, TestEventId, out varAIndex, out varBIndex);
                        varEntityIndex = b.BindVariableToDataIndex("entity");
                        b.BindVariableToInput(varEntityIndex, send.Entity);
                    }),
                new EntityTestSetup(
                    b => { AddOnEventLogIndex(b, TestEventId, 0); }
                ),
                new EntityTestSetup(
                    b => { AddOnEventLogIndex(b, TestEventId, 1); }
                ),
                (manager, entities, index) =>
                {
                    Assert.That(senderEntity, Is.Not.EqualTo(Entity.Null));
                    GraphInstance instance = GetGraphInstance(senderEntity);
                    instance.WriteValueToDataSlot(varAIndex.DataIndex, 42);
                    instance.WriteValueToDataSlot(varBIndex.DataIndex, -4200f);
                    instance.WriteValueToDataSlot(varEntityIndex.DataIndex, targetEntity);
                },
                (manager, entities, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                });
        }

        static SendEvent AddEventSender(GraphBuilder b, ulong eventId, out GraphBuilder.VariableHandle varAIndex, out GraphBuilder.VariableHandle varBIndex)
        {
            var update = b.AddUpdate();
            var send = b.AddNode(new SendEvent { EventId = eventId, Values = new InputDataMultiPort { DataCount = 2 } });
            varAIndex = b.BindVariableToDataIndex("a");
            varBIndex = b.BindVariableToDataIndex("b");
            b.BindVariableToInput(varAIndex, send.Values.SelectPort(0));
            b.BindVariableToInput(varBIndex, send.Values.SelectPort(1));
            b.CreateEdge(update.Output, send.Input);
            return send;
        }

        static OnEvent AddOnEventLogIndex(GraphBuilder b, ulong eventId, uint index)
        {
            return AddOnEventLogIndices(b, eventId, index);
        }

        static OnEvent AddOnEventLogIndices(GraphBuilder b, ulong eventId, params uint[] indices)
        {
            var onEvent = b.AddNode(new OnEvent { EventId = eventId, Values = new OutputDataMultiPort { DataCount = 2 } });
            var log = b.AddLog(indices.Length);
            b.CreateEdge(onEvent.Output, log.Input);
            for (uint i = 0; i < indices.Length; i++)
            {
                b.CreateEdge(onEvent.Values.SelectPort(indices[i]), log.Messages.SelectPort(i));
            }
            return onEvent;
        }
    }
}
