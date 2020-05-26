using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotsStencil;
using NUnit.Framework;
using Runtime;
using Unity.Core;
using Unity.Entities;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScriptingTests;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScriptingECSTests
{
    public abstract class GraphBaseFixture : BaseFixture
    {
        protected static BindingId BindingIdFromString(string s) => BindingId.From((ulong)s.GetHashCode(), 0);

        protected static Log AddLogIntNode(GraphBuilder b, int constantToLog)
        {
            var log = AddLogNode(b);
            var constInt = b.AddNode(new ConstantInt { Value = constantToLog });
            b.CreateEdge(constInt.ValuePort, log.Messages.SelectPort(0));
            return log;
        }

        protected static Log AddLogNode(GraphBuilder b, int numMessages = 1)
        {
            return b.AddNode(new Log { Messages = new InputDataMultiPort { DataCount = numMessages } });
        }

        World m_World;
        protected EntityManager m_EntityManager;
        protected ScriptingGraphRuntime m_System;

        Dictionary<Entity, GraphInstance> m_GraphInstances;

        [SetUp]
        public override void SetUp()
        {
            m_GraphInstances = m_GraphInstances  ?? new Dictionary<Entity, GraphInstance>();
            base.SetUp();

            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
            SetUpWorld();
        }

        [TearDown]
        public override void TearDown()
        {
            m_GraphInstances.Clear();
            TearDownWorld();

            base.TearDown();

            GC.Collect();
        }

        void SetUpWorld()
        {
            m_World = new World("test");
            m_EntityManager = m_World.EntityManager;
            m_System = m_World.CreateSystem<ScriptingGraphRuntime>();
        }

        void TearDownWorld()
        {
            m_World.Dispose();
        }

        public delegate void StepDelegate(EntityManager entityManager, Entity e, int frameIndex);
        public delegate void StepMultiEntitiesDelegate(EntityManager entityManager, List<Entity> graphEntities, int frameIndex);
        public delegate void GraphDefinitionSetupDelegate(GraphBuilder builder, List<ValueInput> inputs);

        StepMultiEntitiesDelegate ApplyOnlyFirstEntity(StepDelegate del)
        {
            return (manager, entities, index) => del(manager, entities[0], index);
        }

        public struct EntityTestSetup
        {
            public GraphDefinitionSetupDelegate GraphSetup;
            public Action<Entity> EntityDefined;

            public EntityTestSetup(GraphDefinitionSetupDelegate graphSetup)
                : this(null, graphSetup)
            {
            }

            public EntityTestSetup(Action<GraphBuilder> graphSetup)
                : this(null, graphSetup)
            {
            }

            public EntityTestSetup(Action<Entity> entityDefined, Action<GraphBuilder> graphSetup)
                : this(entityDefined, (builder, inputs) => graphSetup(builder))
            {
            }

            public EntityTestSetup(Action<Entity> entityDefined, GraphDefinitionSetupDelegate graphSetup)
            {
                GraphSetup = graphSetup;
                EntityDefined = entityDefined;
            }
        }

        protected void SetupTestGraphDefinitionMultipleFrames(Action<GraphBuilder> graphSetup, StepDelegate systemSetup = null, params StepDelegate[] frameChecks)
        {
            SetupMultiEntitiesTestGraph(new List<EntityTestSetup> { new EntityTestSetup(graphSetup) }, ApplyOnlyFirstEntity(systemSetup), frameChecks.Select(ApplyOnlyFirstEntity).ToArray());
        }

        protected void SetupTestGraphDefinitionMultipleFrames(GraphDefinitionSetupDelegate graphSetup, StepDelegate systemSetup = null, params StepDelegate[] frameChecks)
        {
            SetupMultiEntitiesTestGraph(new List<EntityTestSetup> { new EntityTestSetup(graphSetup) }, ApplyOnlyFirstEntity(systemSetup), frameChecks.Select(ApplyOnlyFirstEntity).ToArray());
        }

        protected void SetupMultiEntitiesTestGraph(GraphDefinitionSetupDelegate setupA, GraphDefinitionSetupDelegate setupB, StepMultiEntitiesDelegate systemSetup = null, params StepMultiEntitiesDelegate[] frameChecks)
        {
            SetupMultiEntitiesTestGraph(new EntityTestSetup(setupA), new EntityTestSetup(setupB), systemSetup, frameChecks);
        }

        protected void SetupMultiEntitiesTestGraph(Action<GraphBuilder> setupA, Action<GraphBuilder> setupB, StepMultiEntitiesDelegate systemSetup = null, params StepMultiEntitiesDelegate[] frameChecks)
        {
            SetupMultiEntitiesTestGraph(new EntityTestSetup(setupA), new EntityTestSetup(setupB), systemSetup, frameChecks);
        }

        protected void SetupMultiEntitiesTestGraph(EntityTestSetup setupA, EntityTestSetup setupB, StepMultiEntitiesDelegate systemSetup = null, params StepMultiEntitiesDelegate[] frameChecks)
        {
            SetupMultiEntitiesTestGraph(new List<EntityTestSetup> { setupA, setupB }, systemSetup, frameChecks);
        }

        protected void SetupMultiEntitiesTestGraph(EntityTestSetup setupA, EntityTestSetup setupB, EntityTestSetup setupC, StepMultiEntitiesDelegate systemSetup = null, params StepMultiEntitiesDelegate[] frameChecks)
        {
            SetupMultiEntitiesTestGraph(new List<EntityTestSetup> { setupA, setupB, setupC }, systemSetup, frameChecks);
        }

        protected void SetupMultiEntitiesTestGraph(List<EntityTestSetup> setups, StepMultiEntitiesDelegate systemSetup = null, params StepMultiEntitiesDelegate[] frameChecks)
        {
            // Something fishy is going on here, the TypeManager is throwing a fit when adding new ComponentData through
            // live compilation.  Shutting down the TypeManager and re-initializing seems like the way to circumvent the
            // issue, but it does not seem like it's enough.
            // Tearing the world down (along with the TypeManager), and recreating it, works.
            TearDownWorld();
            SetUpWorld();

            List<GraphDefinition> graphDefinitions = new List<GraphDefinition>(setups.Count);
            List<ValueInput[]> inputArrays = new List<ValueInput[]>(setups.Count);
            try
            {
                var tempInputs = new List<ValueInput>();
                for (var i = 0; i < setups.Count; i++)
                {
                    var builder = new GraphBuilder();
                    tempInputs.Clear();
                    setups[i].GraphSetup(builder, tempInputs);
                    inputArrays.Add(tempInputs.ToArray());
                    graphDefinitions.Add(builder.Build(new global::DotsStencil.DotsStencil()).GraphDefinition);
                }
            }
            finally
            {
                GC.Collect();
            }

            List<Entity> graphEntities = new List<Entity>(setups.Count);

            // first define every entity so that other system can reference them in System setup
            for (int i = 0; i < setups.Count; i++)
            {
                var entity = SetupDefinitionEntity(graphDefinitions[i], inputArrays[i]);
                graphEntities.Add(entity);
                setups[i].EntityDefined?.Invoke(entity);
            }

            systemSetup?.Invoke(m_EntityManager, graphEntities, 0);

            for (int frameIndex = 0; frameIndex < frameChecks.Length; frameIndex++)
            {
                TestGraph();
                frameChecks[frameIndex]?.Invoke(m_EntityManager, graphEntities, frameIndex + 1);
            }
        }

        Entity SetupDefinitionEntity(GraphDefinition graphDefinition, ValueInput[] valueInputs)
        {
            var graphEntity = m_EntityManager.CreateEntity();

            var scriptingGraphAsset = ScriptableObject.CreateInstance<ScriptingGraphAsset>();
            scriptingGraphAsset.hideFlags |= HideFlags.DontSave;
            scriptingGraphAsset.Definition = graphDefinition;
            m_EntityManager.AddSharedComponentData(graphEntity, new ScriptingGraph { ScriptingGraphAsset = scriptingGraphAsset });

            m_EntityManager.AddComponentData(graphEntity, new ScriptingGraphInstance());
            m_EntityManager.AddComponentData(graphEntity, new ScriptingGraphInstanceAlive());
            var inputs = m_EntityManager.AddBuffer<ValueInput>(graphEntity); // add buffer as late as possible because changes to entities can invalidate buffer
            inputs.CopyFrom(valueInputs);
            m_GraphInstances.Add(graphEntity, m_System.CreateEntityContext(inputs, graphEntity, graphDefinition));
            return graphEntity;
        }

        protected GraphDefinition CompileGraph(out CompilationResult results)
        {
            results = default;
            DotsTranslator translator = (DotsTranslator)GraphModel.CreateTranslator();

            var compilationOptions = CompilationOptions.LiveEditing;
            results = GraphModel.Compile(AssemblyType.Memory, translator, compilationOptions);

            var results2 = results;
            Assert.That(results?.status, Is.EqualTo(CompilationStatus.Succeeded),
                () => $"Compilation failed, errors: {String.Join("\n", results2?.errors)}");
            return ((DotsTranslator.DotsCompilationResult)results).GraphDefinition;
        }

        void TestGraph()
        {
            // Force System.OnUpdate to be executed
            var field = typeof(ComponentSystemBase).GetField("m_AlwaysUpdateSystem", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(m_System, true);
            var deltaTime = 0.016f;

            // 1st frame: keep elapsed time to 0, set delta
            m_World.SetTime(new TimeData(m_World.Time.ElapsedTime, deltaTime));
            Assert.DoesNotThrow(m_System.Update);
            // increment elapsed time AFTER update
            m_World.SetTime(new TimeData(m_World.Time.ElapsedTime + deltaTime, deltaTime));

            m_System.Complete();
            var endFrame = m_World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            endFrame.Update();
            endFrame.Complete();
        }

        protected GraphInstance GetGraphInstance(Entity e) => m_GraphInstances[e];
    }
}
