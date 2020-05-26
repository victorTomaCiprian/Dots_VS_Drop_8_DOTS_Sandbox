using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using Unity.Entities;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using ValueType = Runtime.ValueType;

namespace Graphs
{
    public class GraphReferencesRuntimeTests : GraphBaseFixture
    {
        static GraphBuilder.VariableHandle CreateGraphReferenceEntityVariable(GraphBuilder b, List<ValueInput> inputs, Entity referencedEntity)
        {
            var bindingId = nameof(GraphReference.Target).ToBidingId();
            GraphBuilder.VariableHandle entityVariableDataIndex = b.DeclareObjectReferenceVariable(bindingId);
            inputs.Add(new ValueInput { Value = referencedEntity, Index = entityVariableDataIndex.DataIndex });
            return entityVariableDataIndex;
        }

        static GraphReference CreateGraphReferenceFromGrapohAndEntity(GraphBuilder b, GraphDefinition nestedDef, GraphBuilder.VariableHandle entityVariableDataIndex)
        {
            var graphReference = new GraphReference();
            graphReference.Inputs.SetCount(nestedDef.InputTriggers.Count);
            graphReference.Outputs.SetCount(nestedDef.OutputTriggers.Count);
            graphReference.DataInputs.SetCount(nestedDef.InputDatas.Count);
            graphReference.DataOutputs.SetCount(nestedDef.OutputDatas.Count);
            graphReference = b.AddNode(graphReference);
            b.BindVariableToInput(entityVariableDataIndex, graphReference.Target);
            return graphReference;
        }

        protected override bool CreateGraphOnStartup => false;

        ScriptingGraphAsset m_NestedScriptingGraphAsset;
        Mock<TestGraphInstance> m_GraphInstanceMock;

        public override void SetUp()
        {
            base.SetUp();
            m_NestedScriptingGraphAsset = ScriptableObject.CreateInstance<ScriptingGraphAsset>();
            m_NestedScriptingGraphAsset.hideFlags |= HideFlags.DontSave;
        }

        public override void TearDown()
        {
            base.TearDown();
            if (m_NestedScriptingGraphAsset)
                Object.DestroyImmediate(m_NestedScriptingGraphAsset);

            m_GraphInstanceMock?.Object?.Dispose();
        }

        Entity CreateGraphReferenceEntity(GraphDefinition nestedDef)
        {
            var referencedEntity = m_EntityManager.CreateEntity(typeof(ScriptingGraphInstance), typeof(ScriptingGraphInstanceAlive));
            m_EntityManager.AddSharedComponentData(referencedEntity, new ScriptingGraph { ScriptingGraphAsset = m_NestedScriptingGraphAsset });
            m_System.CreateEntityContext(default, referencedEntity, nestedDef);
            return referencedEntity;
        }

        GraphDefinition DefineNestedGraph(Action<GraphBuilder> build)
        {
            var builder = new GraphBuilder();
            build(builder);
            var nestedDef = builder.Build(new global::DotsStencil.DotsStencil()).GraphDefinition;
            m_NestedScriptingGraphAsset.Definition = nestedDef;
            return nestedDef;
        }

        GraphReference SetupNestedGraphAndReference(Action<GraphBuilder> action, GraphBuilder b, List<ValueInput> inputs)
        {
            var nestedDef = DefineNestedGraph(action);
            var referencedEntity = CreateGraphReferenceEntity(nestedDef);
            var entityVariableDataIndex = CreateGraphReferenceEntityVariable(b, inputs, referencedEntity);
            var graphReference = CreateGraphReferenceFromGrapohAndEntity(b, nestedDef, entityVariableDataIndex);
            return graphReference;
        }

        GraphReference SetupSubGraphAndReference(Action<GraphBuilder> action, GraphBuilder b)
        {
            var nestedDef = DefineNestedGraph(action);
            var entityVariableDataIndex = b.AllocateDataIndex();
            var graphReference = CreateGraphReferenceFromGrapohAndEntity(b, nestedDef, new GraphBuilder.VariableHandle {DataIndex = entityVariableDataIndex});
            b.BindSubgraph(entityVariableDataIndex, m_NestedScriptingGraphAsset);
            return graphReference;
        }

        [Test]
        public void TestInputTrigger()
        {
            var builder = new GraphBuilder();

            var input = builder.AddNode(builder.DeclareInputTrigger("I"));
            var log = AddLogNode(builder);

            builder.CreateEdge(input.Output, log.Input);
            var def = builder.Build(new global::DotsStencil.DotsStencil()).GraphDefinition;

            using (GraphInstance graphInstance = GraphInstance.Create(def, m_EntityManager, default))
            {
                graphInstance.TriggerGraphInput("I");
                LogAssert.Expect(LogType.Log, ValueType.Unknown.ToString());
                graphInstance.RunFrame(Entity.Null, default);
            }
        }

        [Test]
        public void GraphReferenceTest()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> (other graph: I -> Log -> O) -> Log
                var graphReference = SetupNestedGraphAndReference(builder =>
                {
                    var log1 = AddLogNode(builder);
                    var input = builder.AddNode(builder.DeclareInputTrigger("I"));
                    var output = builder.AddNode(builder.DeclareOutputTrigger("O"));
                    builder.CreateEdge(input.Output, log1.Input);
                    builder.CreateEdge(log1.Output, output.Input);
                }, b, inputs);

                {
                    var update = b.AddNode(new OnUpdate());
                    var enabled = b.AddNode(new ConstantBool { Value = true });
                    var log = AddLogNode(b);

                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(graphReference.Outputs.SelectPort(0), log.Input);
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, Runtime.ValueType.Unknown.ToString());
                    LogAssert.Expect(LogType.Log, Runtime.ValueType.Unknown.ToString());
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void GraphReferenceDestroyTargetTest()
        {
            Entity targetEntity = Entity.Null;
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> (other graph: I -> Log -> O) -> Log
                var graphReference = SetupNestedGraphAndReference(builder =>
                {
                    var log1 = AddLogNode(builder);
                    var input = builder.AddNode(builder.DeclareInputTrigger("I"));
                    var output = builder.AddNode(builder.DeclareOutputTrigger("O"));
                    builder.CreateEdge(input.Output, log1.Input);
                    builder.CreateEdge(log1.Output, output.Input);
                }, b, inputs);
                targetEntity = inputs.Single().Value.Entity;

                {
                    var update = b.AddNode(new OnUpdate());
                    var enabled = b.AddNode(new ConstantBool { Value = true });
                    var log = AddLogNode(b);

                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(graphReference.Outputs.SelectPort(0), log.Input);
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, Runtime.ValueType.Unknown.ToString());
                    LogAssert.Expect(LogType.Log, Runtime.ValueType.Unknown.ToString());
                },
                (manager, entity, index) =>
                {
                    manager.DestroyEntity(targetEntity);
                    LogAssert.Expect(LogType.Error, "Running nested graph on destroyed entity: Entity(0:1), aborting");
                }, (manager, entity, index) => {
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void GraphReferenceInputOutputDataTest()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> (other graph: IT ---------> Set OD -> OT) -> Log
                //         10 ->(             ID -> Sub 1-/             ) -> Message
                GraphReference graphReference = SetupNestedGraphAndReference(builder =>
                {
                    GraphTriggerInput input = builder.AddNode(builder.DeclareInputTrigger("IT"));
                    GraphTriggerOutput output = builder.AddNode(builder.DeclareOutputTrigger("OT"));

                    GraphDataInput inputData = builder.AddNode(builder.DeclareInputData("ID", ValueType.Int));
                    var outputDataBindingId = BindingIdFromString("OD");
                    var outputDataIndex = builder.BindVariableToDataIndex(outputDataBindingId);
                    builder.DeclareOutputData(outputDataBindingId, ValueType.Int, outputDataIndex);
                    var sub = builder.AddMath(MathGeneratedFunction.SubtractIntInt);
                    var constOne = builder.AddNode(new ConstantInt { Value = 1 });
                    var setVariable = new SetVariable();
                    setVariable = builder.AddNode(setVariable, _ => outputDataIndex.DataIndex);

                    builder.CreateEdge(inputData.Output, sub.Inputs.SelectPort(0));
                    builder.CreateEdge(constOne.ValuePort, sub.Inputs.SelectPort(1));
                    builder.CreateEdge(sub.Result, setVariable.Value);
                    builder.CreateEdge(input.Output, setVariable.Input);
                    builder.CreateEdge(setVariable.Output, output.Input);
                }, b, inputs);

                {
                    OnUpdate update = b.AddNode(new OnUpdate());
                    ConstantBool enabled = b.AddNode(new ConstantBool { Value = true });
                    Log log = AddLogNode(b);
                    ConstantInt constantInt = b.AddNode(new ConstantInt { Value = 10 });

                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(graphReference.Outputs.SelectPort(0), log.Input);
                    b.CreateEdge(graphReference.DataOutputs.SelectPort(0), log.Messages.SelectPort(0));
                    b.CreateEdge(constantInt.ValuePort, graphReference.DataInputs.SelectPort(0));
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, 9.ToString());
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void SubGraphReferenceInputOutputDataTest()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> (other graph: IT ---------> Set OD -> OT) -> Log
                //         10 ->(             ID -> Sub 1-/             ) -> Message
                GraphReference graphReference = SetupSubGraphAndReference(builder =>
                {
                    GraphTriggerInput input = builder.AddNode(builder.DeclareInputTrigger("IT"));
                    GraphTriggerOutput output = builder.AddNode(builder.DeclareOutputTrigger("OT"));

                    GraphDataInput inputData = builder.AddNode(builder.DeclareInputData("ID", ValueType.Int));
                    var outputDataBindingId = BindingIdFromString("OD");
                    var outputDataIndex = builder.BindVariableToDataIndex(outputDataBindingId);
                    builder.DeclareOutputData(outputDataBindingId, ValueType.Int, outputDataIndex);
                    var sub = builder.AddNode(new MathGenericNode().WithFunction(MathGeneratedFunction.SubtractIntInt));
                    var constOne = builder.AddNode(new ConstantInt { Value = 1 });
                    var setVariable = new SetVariable();
                    setVariable = builder.AddNode(setVariable, _ => outputDataIndex.DataIndex);

                    builder.CreateEdge(inputData.Output, sub.Inputs.SelectPort(0));
                    builder.CreateEdge(constOne.ValuePort, sub.Inputs.SelectPort(1));
                    builder.CreateEdge(sub.Result, setVariable.Value);
                    builder.CreateEdge(input.Output, setVariable.Input);
                    builder.CreateEdge(setVariable.Output, output.Input);
                }, b);

                {
                    OnUpdate update = b.AddNode(new OnUpdate());
                    ConstantBool enabled = b.AddNode(new ConstantBool { Value = true });
                    Log log = AddLogNode(b);
                    ConstantInt constantInt = b.AddNode(new ConstantInt { Value = 10 });

                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(graphReference.Outputs.SelectPort(0), log.Input);
                    b.CreateEdge(graphReference.DataOutputs.SelectPort(0), log.Messages.SelectPort(0));
                    b.CreateEdge(constantInt.ValuePort, graphReference.DataInputs.SelectPort(0));
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, 9.ToString());
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void GraphReferenceInputOutputDataNoOutputTriggerTest()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> (other graph: IT ---------> Set OD)
                //        10 -> (             ID -> Sub 1-/       ) -> Log Message
                // On Update -> Log

                GraphReference graphReference = SetupNestedGraphAndReference(builder =>
                {
                    GraphTriggerInput input = builder.AddNode(builder.DeclareInputTrigger("IT"));

                    GraphDataInput inputData = builder.AddNode(builder.DeclareInputData("ID", ValueType.Int));
                    var outputDataBindingId = BindingIdFromString("OD");
                    var outputDataIndex = builder.BindVariableToDataIndex(outputDataBindingId);
                    builder.DeclareOutputData(outputDataBindingId, ValueType.Int, outputDataIndex);
                    var sub = builder.AddMath(MathGeneratedFunction.SubtractIntInt);
                    var constOne = builder.AddNode(new ConstantInt { Value = 1 });
                    var setVariable = new SetVariable();
                    setVariable = builder.AddNode(setVariable, _ => outputDataIndex.DataIndex);

                    builder.CreateEdge(inputData.Output, sub.Inputs.SelectPort(0));
                    builder.CreateEdge(constOne.ValuePort, sub.Inputs.SelectPort(1));
                    builder.CreateEdge(sub.Result, setVariable.Value);
                    builder.CreateEdge(input.Output, setVariable.Input);
                }, b, inputs);

                {
                    OnUpdate update = b.AddNode(new OnUpdate());
                    ConstantBool enabled = b.AddNode(new ConstantBool { Value = true });
                    Log log = AddLogNode(b);
                    ConstantInt constantInt = b.AddNode(new ConstantInt { Value = 10 });

                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(update.Output, log.Input);
                    b.CreateEdge(graphReference.DataOutputs.SelectPort(0), log.Messages.SelectPort(0));
                    b.CreateEdge(constantInt.ValuePort, graphReference.DataInputs.SelectPort(0));
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, 9.ToString());
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void GraphReferenceExecutionOrderTest()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> (other graph: I ---> Log 1 -> O1) -> Log 2
                //              (                \-> Log 3 -> O2) -> Log 4
                var graphReference = SetupNestedGraphAndReference(builder =>
                {
                    var log1 = AddLogIntNode(builder, 1);
                    var log3 = AddLogIntNode(builder, 3);
                    var input = builder.AddNode(builder.DeclareInputTrigger("I"));
                    var output1 = builder.AddNode(builder.DeclareOutputTrigger("O1"));
                    var output2 = builder.AddNode(builder.DeclareOutputTrigger("O2"));
                    builder.CreateEdge(input.Output, log1.Input);
                    builder.CreateEdge(input.Output, log3.Input);
                    builder.CreateEdge(log1.Output, output1.Input);
                    builder.CreateEdge(log3.Output, output2.Input);
                }, b, inputs);

                {
                    var update = b.AddNode(new OnUpdate());
                    var enabled = b.AddNode(new ConstantBool { Value = true });
                    var log2 = AddLogIntNode(b, 2);
                    var log4 = AddLogIntNode(b, 4);

                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(graphReference.Outputs.SelectPort(0), log2.Input);
                    b.CreateEdge(graphReference.Outputs.SelectPort(1), log4.Input);
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "1");
                    LogAssert.Expect(LogType.Log, "2");
                    LogAssert.Expect(LogType.Log, "3");
                    LogAssert.Expect(LogType.Log, "4");
                    LogAssert.NoUnexpectedReceived();
                });
        }

        [Test]
        public void GraphReferenceNestedWaitTest()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                // On Update -> Once -> (other graph: I -> Wait 1 -> Log -> O) -> Log
                var graphReference = SetupNestedGraphAndReference(builder =>
                {
                    var log = AddLogNode(builder);
                    var wait = builder.AddNode(new Wait());
                    var waitDuration = builder.AddNode(new ConstantFloat { Value = 0.01f });
                    var input = builder.AddNode(builder.DeclareInputTrigger("I"));
                    var output = builder.AddNode(builder.DeclareOutputTrigger("O"));
                    builder.CreateEdge(input.Output, wait.Start);
                    builder.CreateEdge(wait.OnDone, log.Input);
                    builder.CreateEdge(log.Output, output.Input);
                    builder.CreateEdge(waitDuration.ValuePort, wait.Duration);
                }, b, inputs);
                {
                    var update = b.AddNode(new OnUpdate());
                    var once = b.AddNode(new Once());
                    var log = AddLogNode(b);
                    var enabled = b.AddNode(new ConstantBool { Value = true });
                    b.CreateEdge(enabled.ValuePort, update.Enabled);
                    b.CreateEdge(update.Output, once.Input);
                    b.CreateEdge(once.Output, graphReference.Inputs.SelectPort(0));
                    b.CreateEdge(graphReference.Outputs.SelectPort(0), log.Input);
                }
            },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                },
                (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, Runtime.ValueType.Unknown.ToString());
                    LogAssert.Expect(LogType.Log, Runtime.ValueType.Unknown.ToString());
                    LogAssert.NoUnexpectedReceived();
                });
        }
    }
}
