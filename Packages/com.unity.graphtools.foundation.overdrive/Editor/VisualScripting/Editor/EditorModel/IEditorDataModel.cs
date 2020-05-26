using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [Serializable]
    public struct OpenedGraph
    {
        public GraphAssetModel GraphAssetModel;
        public GameObject BoundObject;

        public OpenedGraph(GraphAssetModel graphAssetModel, GameObject boundObject)
        {
            GraphAssetModel = graphAssetModel;
            BoundObject = boundObject;
        }
    }
    public interface IEditorDataModel
    {
        UpdateFlags UpdateFlags { get; }
        IGraphElementModel ElementModelToRename { get; set; }
        GUID NodeToFrameGuid { get; set; }
        int CurrentGraphIndex { get; }
        VSPreferences Preferences { get; }
        GameObject BoundObject { get; set; }
        IPluginRepository PluginRepository { get; }
        List<OpenedGraph> PreviousGraphModels { get; }
        int UpdateCounter { get; set; }
        bool TracingEnabled { get; set; }

        void SetUpdateFlag(UpdateFlags flag);

        void RequestCompilation(RequestCompilationOptions options);

        bool ShouldSelectElementUponCreation(IHasGraphElementModel hasGraphElementModel);

        void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select);

        void ClearElementsToSelectUponCreation();

        bool ShouldExpandBlackboardRowUponCreation(string rowName);

        void ExpandBlackboardRowsUponCreation(IEnumerable<string> rowNames, bool expand);

        bool ShouldExpandElementUponCreation(IVisualScriptingField visualScriptingField);

        void ExpandElementsUponCreation(IEnumerable<IVisualScriptingField> visualScriptingFields, bool expand);
    }

    public enum RequestCompilationOptions
    {
        Default,
        SaveGraph,
    }
}
