using System;
using Runtime;
using UnityEditor;
using UnityEngine;

namespace DotsStencil
{
    [CustomEditor(typeof(ScriptingGraphAsset))]
    class ScriptingGraphAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (ScriptingGraphAsset)target;
            var def = asset.Definition;
            for (var index = 0; index < def.NodeTable.Count; index++)
            {
                NodeId nodeId = new NodeId((uint)index);
                INode node = def.NodeTable[index];
                var nodeType = node.GetType();
                if (EditorGUILayout.Foldout(true, $"{index+1} {nodeType.Name}"))
                {
                    EditorGUI.indentLevel++;
                    foreach (var fieldInfo in BaseDotsNodeModel.GetNodePorts(nodeType))
                    {
                        IPort port = fieldInfo.GetValue(node) as IPort;
                        var portIndex = port.GetPort().Index;
                        var portInfo = def.PortInfoTable[(int)portIndex];
                        // portInfo.

                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                        {
                            EditorGUILayout.PrefixLabel($"{portIndex} {fieldInfo.Name}");
                            var isInput = port is IInputDataPort || port is IInputTriggerPort;
                            string io = isInput ? "I" : "O";
                            var  isData = port is IInputDataPort || port is IOutputDataPort;
                            string dt = isData ? "D" : "T";
                            string m = (port is IMultiDataPort multiPort)
                                ? "M" + multiPort.GetDataCount()
                                : String.Empty;
                            var portInfoDataIndex = isData && def.HasConnectedValue(port) ? portInfo.DataIndex.ToString() : "";
                            EditorGUILayout.LabelField($"{io}\t{dt}\t{m}\t{portInfoDataIndex}");
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
