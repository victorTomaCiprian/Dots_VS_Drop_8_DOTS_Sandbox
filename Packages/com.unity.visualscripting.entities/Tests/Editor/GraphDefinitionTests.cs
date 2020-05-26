using NUnit.Framework;
using Runtime;
using DotsStencil = DotsStencil.DotsStencil;

namespace UnityEditor.VisualScriptingECSTests
{
    public class GraphDefinitionTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void TestDataPortsConnections(bool connected)
        {
            var builder = new GraphBuilder();
            var getChildAt = builder.AddNode(new GetChildAt());
            var log = builder.AddNode(new Log { Messages = new InputDataMultiPort { DataCount = 1 } });

            if (connected)
                builder.CreateEdge(getChildAt.Child, log.Messages.SelectPort(0));

            var graphDefinition = builder.Build(new global::DotsStencil.DotsStencil()).GraphDefinition;
            Assert.AreEqual(graphDefinition.HasConnectedValue(getChildAt.Child), connected);
            Assert.AreEqual(graphDefinition.HasConnectedValue(log.Messages.SelectPort(0)), connected);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestOutputTriggerPortConnections(bool connected)
        {
            var builder = new GraphBuilder();
            var onUpdate = builder.AddNode(new OnUpdate());
            var log = builder.AddNode(new Log());

            if (connected)
                builder.CreateEdge(onUpdate.Output, log.Input);

            var graphDefinition = builder.Build(new global::DotsStencil.DotsStencil()).GraphDefinition;
            Assert.AreEqual(graphDefinition.HasConnectedValue(onUpdate.Output), connected);
        }
    }
}
