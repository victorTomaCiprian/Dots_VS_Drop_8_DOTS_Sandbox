using System;
using NUnit.Framework;

namespace Nodes
{
    public class GraphBuilderTests
    {
        [Test]
        public void IdenticalStringConstantsAreOnlyAddedOnce()
        {
            GraphBuilder builder = new GraphBuilder();
            string constStr = "asd";
            var index1 = builder.StoreStringConstant(constStr);

            var index2 = builder.StoreStringConstant(constStr);
            Assert.AreEqual(index1, index2);

            var def = builder.Build(new global::DotsStencil.DotsStencil());
            Assert.AreEqual(1, def.GraphDefinition.Strings.Count);
        }
    }
}
