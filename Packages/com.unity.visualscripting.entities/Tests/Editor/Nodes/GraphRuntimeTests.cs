using System;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine;
using UnityEngine.TestTools;
using ValueType = Runtime.ValueType;

namespace Graphs
{
    public class GraphRuntimeTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void UpdateSetVariable()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                b.BindVariableToDataIndex("a");
                b.BindVariableToDataIndex("b");
                var varIndex = b.BindVariableToDataIndex("c");
                var constBool = b.AddNode(new ConstantBool { Value = true });
                var onUpdate = b.AddNode(new OnUpdate());
                var setVar = b.AddNode(new SetVariable(), _ => varIndex.DataIndex);
                var log = AddLogNode(b);
                var constFloat = b.AddNode(new ConstantFloat { Value = 41 });

                b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, setVar.Input);
                b.CreateEdge(setVar.Output, log.Input);
                b.CreateEdge(constFloat.ValuePort, setVar.Value);
                b.BindVariableToInput(varIndex, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "41");
                });
        }

        [Test]
        public void UpdateSetVariableDifferentTypesOfVariableAndValue()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                b.BindVariableToDataIndex("a");
                b.BindVariableToDataIndex("b");
                var varIndex = b.BindVariableToDataIndex("c");
                var constBool = b.AddNode(new ConstantBool { Value = true });
                var onUpdate = b.AddNode(new OnUpdate());
                var setVar = b.AddNode(new SetVariable(){VariableType = ValueType.Int}, _ => varIndex.DataIndex);
                var log = AddLogNode(b);
                var constFloat = b.AddNode(new ConstantFloat { Value = 41.3f });

                b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, setVar.Input);
                b.CreateEdge(setVar.Output, log.Input);
                b.CreateEdge(constFloat.ValuePort, setVar.Value);
                b.BindVariableToInput(varIndex, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "41");
                });
        }

        [Test]
        public void UpdateLogVariable()
        {
            GraphBuilder.VariableHandle varIndex = default;
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                var constBool = b.AddNode(new ConstantBool { Value = true });
                varIndex = b.BindVariableToDataIndex("a");
                var onUpdate = b.AddNode(new OnUpdate());
                var log = AddLogNode(b);

                b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.BindVariableToInput(varIndex, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(varIndex.DataIndex, 42);
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                });
        }

        [Test]
        public void UpdateLogVariable2()
        {
            GraphBuilder.VariableHandle varIndexA = default;
            GraphBuilder.VariableHandle varIndexB = default;
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                var constBool = b.AddNode(new ConstantBool { Value = true });
                varIndexA = b.BindVariableToDataIndex("a");
                varIndexB = b.BindVariableToDataIndex("b");
                var onUpdate = b.AddNode(new OnUpdate());
                var log = AddLogNode(b);

                b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.BindVariableToInput(varIndexB, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(varIndexA.DataIndex, 41);
                    instance.WriteValueToDataSlot(varIndexB.DataIndex, 42);
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                });
        }

        [Test]
        public void TestUpdateLogConstant()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                var constBool = b.AddNode(new ConstantBool { Value = true });
                var onUpdate = b.AddNode(new OnUpdate());
                var log = AddLogNode(b);
                var constFloat = b.AddNode(new ConstantFloat { Value = 42 });
                b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.CreateEdge(constFloat.ValuePort, log.Messages.SelectPort(0));
            }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                });
        }

        [Test]
        public void TestUpdateLogSubConstants()
        {
            SetupTestGraphDefinitionMultipleFrames((b, inputs) =>
            {
                var constBool = b.AddNode(new ConstantBool { Value = true });
                var onUpdate = b.AddNode(new OnUpdate());
                var log = AddLogNode(b);
                var sub = b.AddMath(MathGeneratedFunction.SubtractFloatFloat);
                var constFloat1 = b.AddNode(new ConstantFloat { Value = 43 });
                var constFloat2 = b.AddNode(new ConstantFloat { Value = 1 });
                b.CreateEdge(constBool.ValuePort, onUpdate.Enabled);
                b.CreateEdge(onUpdate.Output, log.Input);
                b.CreateEdge(sub.Result, log.Messages.SelectPort(0));
                b.CreateEdge(constFloat1.ValuePort, sub.Inputs.SelectPort(0));
                b.CreateEdge(constFloat2.ValuePort, sub.Inputs.SelectPort(1));
            }, (manager, entity, index) =>
                {
                    LogAssert.NoUnexpectedReceived();
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                });
        }
    }
}
