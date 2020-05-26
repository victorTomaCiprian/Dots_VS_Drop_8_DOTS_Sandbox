using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elements;
using Unity.GraphElements;
using UnityEditor;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.Assertions;

namespace DotsStencil
{
    public class GenerateNodeDocPlugin : IPluginHandler
    {
        public void Register(Store store, GraphView graphView)
        {
        }

        public void Unregister()
        {
        }

        public void OptionsMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Generate Node Documentation"), false, GenerateNodeDoc);
        }

        private void GenerateNodeDoc()
        {
            const string docPath = UIHelper.NodeDocumentationPath;
            if (Directory.Exists(docPath))
                Directory.Delete(docPath, true);
            Directory.CreateDirectory(docPath);

            var gam = GraphAssetModel.Create("Doc", null, typeof(VSGraphAssetModel), false);
            var stateCurrentGraphModel = gam.CreateGraph<VSGraphModel>("Doc", typeof(DotsStencil), false);
            var stencil = stateCurrentGraphModel.Stencil;
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases();

            HashSet<string> fileNames = new HashSet<string>();
            foreach (var searcherItem in dbs[0].ItemList.OfType<GraphNodeModelSearcherItem>())
            {
                var graphElementModels = GraphNodeSearcherAdapter.CreateGraphElementModels(stateCurrentGraphModel,  searcherItem);
                if (graphElementModels.Count() != 1)
                    continue;

                var model = graphElementModels.Single();
                if (model is IDotsNodeModel dotsNodeModel)
                {
                    var formatter = new MarkdownNodeDocumentationFormatter();

                    formatter.DocumentNode(searcherItem, dotsNodeModel);
                    var fileName = string.IsNullOrWhiteSpace(dotsNodeModel.Title) ? dotsNodeModel.GetType().Name : dotsNodeModel.Title;
                    Assert.IsTrue(fileNames.Add(fileName), "Duplicate filename: " + fileName);
                    var filePath = Path.Combine(docPath, $"{fileName}.md");
                    var contents = formatter.ToString();
                    if (!File.Exists(filePath) || File.ReadAllText(filePath) != contents)
                        File.WriteAllText(filePath, contents);
                }
            }
        }
    }
}
