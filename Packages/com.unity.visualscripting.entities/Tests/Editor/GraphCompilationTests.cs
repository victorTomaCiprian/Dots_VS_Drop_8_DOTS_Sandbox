using System;
using DotsStencil;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VisualScriptingECSTests
{
    public class GraphCompilationTests : GraphBaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void TestGraphmodelNodesGenerateNodes()
        {
            GraphModel.CreateNode<OnUpdateNodeModel>("Update");
            var def = CompileGraph(out var results);
            // Also creates a constant bool node for the Enabled input
            Assert.AreEqual(2, def.NodeTable.Count);
        }
    }
}
