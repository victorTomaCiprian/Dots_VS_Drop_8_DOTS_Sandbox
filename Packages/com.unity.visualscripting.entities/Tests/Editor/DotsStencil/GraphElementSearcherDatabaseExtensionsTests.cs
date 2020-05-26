using System.Linq;
using NodeModels;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScriptingTests;
using UnityEngine;

namespace DotsStencil
{
    public class GraphElementSearcherDatabaseExtensionsTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void TestAddNodesWithSearcherItemCollectionAttribute()
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddNodesWithSearcherItemCollectionAttribute()
                .Build();

            var results = db.Search("negate", out _);
            var item = (ISearcherItemDataProvider)results[0];
            var data = (NodeSearcherItemData)item.Data;

            Assert.AreEqual(SearcherItemTarget.Node, data.Target);
            Assert.AreEqual(typeof(MathNodeModel), data.Type);

            var nodes = ((GraphNodeModelSearcherItem)item).CreateElements.Invoke(
                new GraphNodeCreationData(GraphModel, Vector2.zero));

            Assert.AreEqual(1, nodes.Length);

            var negateNode = (MathNodeModel)nodes.First();
            Assert.AreEqual("negate", negateNode.TypedNode.Function.GetMethodsSignature().OpType.ToLower());
        }
    }
}
