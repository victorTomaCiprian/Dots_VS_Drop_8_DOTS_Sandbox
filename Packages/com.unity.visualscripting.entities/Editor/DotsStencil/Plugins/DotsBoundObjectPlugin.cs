using Elements;
using Unity.GraphElements;
using UnityEditor;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEngine.UIElements;

namespace DotsStencil
{
    public class DotsBoundObjectPlugin : IPluginHandler
    {
        private const string k_WarningPlayMode = "Warning: during Play Mode, scene references cannot be edited";
        private const string k_WarningEditAsset = "Warning: when editing a project asset and not a scene object, scene references cannot be edited";
        private Store m_Store;
        private GraphView m_GraphView;
        private Label m_WarningLabel;

        public void Register(Store store, GraphView graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            EditorApplication.update += Update;
            CreateWarningLabel();
            Update();
        }

        private void CreateWarningLabel()
        {
            if (m_WarningLabel == null)
            {
                m_WarningLabel = new Label();
                m_WarningLabel.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIHelper.TemplatePath + "WarningLabel.uss"));
                m_WarningLabel.AddToClassList("dots-warning-label");
            }
        }

        private void ShowWarningLabel(string content)
        {
            if (m_GraphView == null)
                return;
            CreateWarningLabel();
            if (m_WarningLabel.parent != m_GraphView)
                m_GraphView.Add(m_WarningLabel);
            m_WarningLabel.text = content;
        }

        private void HideWarningLabel()
        {
            if (m_GraphView == null)
                return;
            m_WarningLabel?.RemoveFromHierarchy();
        }

        private void Update()
        {
            if (m_Store == null)
                return;
            if (EditorApplication.isPlaying)
                ShowWarningLabel(k_WarningPlayMode);
            else if (((DotsStencil)m_Store.GetState().CurrentGraphModel.Stencil).Type == DotsStencil.GraphType.Object && (!(m_Store?.GetState()?.EditorDataModel?.BoundObject is UnityEngine.Object obj) || !obj))
                ShowWarningLabel(k_WarningEditAsset);
            else
                HideWarningLabel();
        }

        public void Unregister()
        {
            EditorApplication.update -= Update;
            HideWarningLabel();
            m_Store = null;
            m_GraphView = null;
        }

        public void OptionsMenu(GenericMenu menu)
        {
        }
    }
}
