using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphElements;
using UnityEditor;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.VisualScripting.Editor.Blackboard;

namespace DotsStencil
{
    public class DotsBlackboardProvider : IBlackboardProvider
    {
        const string k_ObjectGraphTitle = "Scripting Graph";
        const string k_SubgraphTitle = "Scripting Subgraph";

        const int k_GraphDeclarationsSection = 0;
        const string k_GraphDeclarationsSectionTitle = "Variables";
        const int k_InputPortDeclarationsSection = 1;
        const string k_InputPortDeclarationsSectionTitle = "Triggers";

        readonly DotsStencil m_Stencil;
        Store m_Store;
        Blackboard m_Blackboard;

        public DotsBlackboardProvider(DotsStencil stencil)
        {
            m_Stencil = stencil;
        }

        public string GetSubTitle() => m_Stencil.Type == DotsStencil.GraphType.Object ? k_ObjectGraphTitle : k_SubgraphTitle;

        public IEnumerable<BlackboardSection> CreateSections()
        {
            BlackboardSection CreateSection(string sectionTitle, bool isData)
            {
                var section = new BlackboardSection {title = sectionTitle };
                var queriesSectionHeader = section.Q("sectionHeader");
                Button button;
                queriesSectionHeader.Add(button = new Button() { name = "addButton", text = "+" });

                button.clickable.clickedWithEventInfo += e =>
                {
                    var menu = new GenericMenu();
                    if (isData)
                    {
                        menu.AddItem(new GUIContent("Local Variable"), false, _ => CreateDeclaration(TypeHandle.Float, "variable", ModifierFlags.None), null);
                        menu.AddItem(new GUIContent("Input Variable"), false, _ => CreateDeclaration(TypeHandle.Float, "data input", ModifierFlags.ReadOnly), null);
                        menu.AddItem(new GUIContent("Output Variable"), false, _ => CreateDeclaration(TypeHandle.Float, "data output", ModifierFlags.WriteOnly), null);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Input Trigger"), false, _ => CreateDeclaration(TypeHandle.ExecutionFlow, "input trigger", ModifierFlags.ReadOnly), null);
                        menu.AddItem(new GUIContent("Output Trigger"), false, _ => CreateDeclaration(TypeHandle.ExecutionFlow, "output trigger", ModifierFlags.WriteOnly), null);
                    }
                    menu.DropDown(new Rect(e.originalMousePosition, Vector2.zero));
                };
                return section;

                void CreateDeclaration(TypeHandle type, string newItemName, ModifierFlags modifiers)
                {
                    string finalName = newItemName;
                    int i = 0;
                    while (((VSGraphModel)m_Store.GetState().CurrentGraphModel).GraphVariableModels.Any(v => v.Name == finalName))
                        finalName = newItemName + i++;
                    m_Store.Dispatch(new CreateGraphVariableDeclarationAction(finalName, true, type, modifiers));
                }
            }

            yield return CreateSection(k_GraphDeclarationsSectionTitle, true);
            yield return CreateSection(k_InputPortDeclarationsSectionTitle, false);
        }

        public void AddItemRequested<TAction>(Store store, TAction action) where TAction : IAction
        {
            throw new NotImplementedException();
        }

        public void MoveItemRequested(Store store, int index, VisualElement field)
        {
            throw new NotImplementedException();
        }

        public void RebuildSections(Blackboard blackboard)
        {
            m_Blackboard = blackboard;
            m_Store = blackboard.Store;
            var currentGraphModel = (VSGraphModel)blackboard.Store.GetState().CurrentGraphModel;

            blackboard.ClearContents();

            if (blackboard.Sections != null && blackboard.Sections.Count > 1)
            {
                blackboard.Sections[k_GraphDeclarationsSection].title = k_GraphDeclarationsSectionTitle;
                blackboard.Sections[k_InputPortDeclarationsSection].title = k_InputPortDeclarationsSectionTitle;
            }

            foreach (VariableDeclarationModel declaration in currentGraphModel.VariableDeclarations)
            {
                var blackboardField = new BlackboardVariableField(blackboard.Store, declaration, blackboard.GraphView);
                var blackboardVariablePropertyView = new DotsVariablePropertyView(blackboard.Store, declaration, blackboard.Rebuild, m_Stencil);
                if (declaration.DataType != TypeHandle.ExecutionFlow)
                {
                    blackboardVariablePropertyView = (DotsVariablePropertyView)blackboardVariablePropertyView.WithLocalInputOutputToggle().WithTypeSelector();
                    if (declaration.IsGraphVariable())
                        blackboardVariablePropertyView = (DotsVariablePropertyView)blackboardVariablePropertyView.WithInitializationField();
                }
                blackboardVariablePropertyView = (DotsVariablePropertyView)blackboardVariablePropertyView.WithTooltipField();
                var blackboardRow = new BlackboardRow(
                    blackboardField,
                    blackboardVariablePropertyView)
                {
                    userData = declaration,
                    expanded = true, // TODO not pretty
                };
                if (blackboard.Sections != null)
                {
                    if (declaration.IsInputOrOutputTrigger())
                        blackboard.Sections[k_InputPortDeclarationsSection].Add(blackboardRow);
                    else
                        blackboard.Sections[k_GraphDeclarationsSection].Add(blackboardRow);
                }
                blackboard.GraphVariables.Add(blackboardField);
            }
        }

        public void DisplayAppropriateSearcher(Vector2 mousePosition, Blackboard blackboard)
        {
            throw new NotImplementedException();
        }

        public bool CanAddItems => false;

        public void BuildContextualMenu(DropdownMenu evtMenu, VisualElement visualElement, Store store, Vector2 mousePosition)
        {
        }
    }

    class DotsVariablePropertyView : BlackboardVariablePropertyView
    {
        public DotsVariablePropertyView(Store store, IVariableDeclarationModel variableDeclarationModel, Blackboard.RebuildCallback rebuildCallback, Stencil stencil)
            : base(store, variableDeclarationModel, rebuildCallback, stencil) {}

        public enum ExposedAsType
        {
            Local,
            Input,
            Output,
        }

        public BlackboardVariablePropertyView WithLocalInputOutputToggle()
        {
            ExposedAsType exposedAsType = ExposedAsType.Local;
            switch (VariableDeclarationModel.Modifiers)
            {
                case ModifierFlags.None:
                    break;
                case ModifierFlags.ReadOnly:
                    exposedAsType = ExposedAsType.Input;
                    break;
                case ModifierFlags.WriteOnly:
                    exposedAsType = ExposedAsType.Output;
                    break;
                default:
                    return this; // object references
            }

            var typeButton = new Label(exposedAsType.ToString());
            AddRow("Exposed as", typeButton);

            return this;
        }
    }
}
