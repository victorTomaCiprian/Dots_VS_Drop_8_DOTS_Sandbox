using System;
using DotsStencil;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using LogNodeModel = DotsStencil.LogNodeModel;

namespace UnityEditor.VisualScriptingECSTests
{
    public class BaseLineTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void SystemTestingWorks()
        {
            GraphBuilder.VariableHandle varIndex = default;
            SetupTestGraphDefinitionMultipleFrames(b =>
            {
                var update = b.AddUpdate();
                var log = b.AddLog();
                varIndex = b.BindVariableToDataIndex("a");
                b.BindVariableToInput(varIndex, log.Messages.SelectPort(0));
                b.CreateEdge(update.Output, log.Input);
            }, (manager, entity, index) =>
                {
                    GraphInstance instance = GetGraphInstance(entity);
                    instance.WriteValueToDataSlot(varIndex.DataIndex, 42);
                    LogAssert.NoUnexpectedReceived();
                }, (manager, entity, index) =>
                {
                    LogAssert.Expect(LogType.Log, "42");
                });
        }
    }
}
