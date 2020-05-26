using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using UnityEditor;
using Unity.GraphElements;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;
using Node = UnityEditor.VisualScripting.Editor.Node;
using Port = UnityEditor.VisualScripting.Editor.Port;

namespace DotsStencil
{
    class DotsVariableToken : Token
    {
        public DotsVariableToken(INodeModel model, Store store, Port input, Port output, GraphView graphView, Texture2D icon = null)
            : base(model, store, input, output, graphView, icon)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIHelper.TemplatePath + "SetVariableNode.uss"));
            AddToClassList("dots-variable-token");
        }
    }

    class SetVariableNode : Node
    {
        class VariableSearcherItem : SearcherItem
        {
            public IVariableDeclarationModel declarationModel { get; }

            public VariableSearcherItem(IVariableDeclarationModel declarationModel)
                : base(declarationModel.Name)
            {
                this.declarationModel = declarationModel;
            }
        }

        public SetVariableNode(SetVariableNodeModel model, Store store, GraphView builderGraphView)
            : base(model, store, builderGraphView)
        {
            // make it clear the ux is not final
            this.Q("title").style.backgroundColor = new StyleColor(new Color32(147, 15, 109, 255));
            this.Q<Label>("title-label").text = "Set ";

            var pill = new Pill();
            var label = pill.Q<Label>("title-label");
            label.text = model.DeclarationModel?.Name ?? "<Pick a variable>";

            var pillContainer = new VisualElement();
            pillContainer.AddToClassList("token");
            pillContainer.style.justifyContent = Justify.Center;
            pillContainer.style.flexGrow = 1;
            pillContainer.Add(pill);

            titleContainer.Insert(1, pillContainer);
            titleContainer.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Token.uss"));

            pill.RegisterCallback<MouseDownEvent>(_ =>
            {
                SearcherWindow.Show(EditorWindow.focusedWindow, ((VSGraphModel)model.GraphModel).GraphVariableModels.Where(g => GraphBuilder.GetVariableType(g) == GraphBuilder.VariableType.Variable)
                    .Select(v => (SearcherItem) new VariableSearcherItem(v)).ToList(), "Pick a variable to set", item =>
                    {
                        var variableSearcherItem = (item as VariableSearcherItem);
                        if (variableSearcherItem == null)
                            return true;
                        model.DeclarationModel = variableSearcherItem.declarationModel;
                        model.DefineNode();
                        store.Dispatch(new RefreshUIAction(UpdateFlags.GraphTopology));
                        return true;
                    }, Event.current.mousePosition);
            });
        }
    }
}
