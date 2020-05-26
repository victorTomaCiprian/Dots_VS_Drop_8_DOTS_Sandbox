using System;
using System.Collections.Generic;
using System.Linq;
using Runtime;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace DotsStencil
{
    interface IGraphReferenceNodeModel : INodeModel
    {
        bool NeedsUpdate();
        VSGraphAssetModel GraphReference { get; }
        GameObject GetBoundObject(IEditorDataModel editorDataModel);
    }
    public class SubgraphReferenceNodeModel : NodeModel, IDotsNodeModel, IGraphReferenceNodeModel
    {
        [SerializeField] GraphReferenceData m_GraphReferenceData = new GraphReferenceData();

        public Type NodeType => m_GraphReferenceData.NodeType;
        public INode Node => m_GraphReferenceData.m_Node;
        public Dictionary<string, uint> PortToOffsetMapping => m_GraphReferenceData.m_PortToOffsetMapping;

        public VSGraphAssetModel GraphReference
        {
            get => m_GraphReferenceData.m_GraphReference;
            set
            {
                m_GraphReferenceData.m_GraphReference = value;
                DefineNode();
            }
        }

        public GameObject GetBoundObject(IEditorDataModel editorDataModel) => null;

        public override bool HasProgress => m_GraphReferenceData.HasProgress;

        public override string Title => m_GraphReferenceData.m_GraphReference
        ? m_GraphReferenceData.m_GraphReference.Name
        : "<Missing Subgraph>";

        protected override void OnDefineNode()
        {
            if (m_GraphReferenceData == null)
                m_GraphReferenceData = new GraphReferenceData();
            m_GraphReferenceData.OnDefineNode(
                (portName, typeHandle) =>  AddDataInputPort(portName, typeHandle),
                (portName, typeHandle) =>  AddDataOutputPort(portName, typeHandle),
                (portName) => AddExecutionInputPort(portName),
                (portName) => AddExecutionOutputPort(portName));
        }

        public bool NeedsUpdate() => m_GraphReferenceData.NeedsUpdate();
    }

    [Serializable]
    class GraphReferenceData
    {
        [SerializeField] public VSGraphAssetModel m_GraphReference;

        public Dictionary<string, uint> m_PortToOffsetMapping;
        public GraphReference m_Node;
        [SerializeField] uint m_LastHashCode;
        public Type NodeType => typeof(GraphReference);
        public bool HasProgress => m_Node is INodeReportProgress;


        uint GetGraphReferenceHashCode()
        {
            return (m_GraphReference?.GraphModel?.Stencil as DotsStencil)?.CompiledScriptingGraphAsset.HashCode ?? 0;
        }

        public bool NeedsUpdate() => m_LastHashCode != GetGraphReferenceHashCode();

        public void OnDefineNode(
            Func<string, TypeHandle, PortModel>
            addDataInputPort, Func<string, TypeHandle, PortModel> addDataOutputPort,
            Func<string, PortModel> addExecutionInputPort,
            Func<string, PortModel> addExecutionOutputPort)
        {
            if (m_PortToOffsetMapping == null)
                m_PortToOffsetMapping = new Dictionary<string, uint>();
            else
                m_PortToOffsetMapping.Clear();
            m_Node = new GraphReference();

            m_Node.Inputs.SetCount(0);
            m_Node.DataInputs.SetCount(0);
            m_Node.Outputs.SetCount(0);
            m_Node.DataOutputs.SetCount(0);
            if (!m_GraphReference)
            {
                return;
            }

            m_LastHashCode = GetGraphReferenceHashCode();

            uint i = 1;
            m_Node.Target.Port.Index = i++;

            var vsGraphModel = (VSGraphModel)m_GraphReference.GraphModel;
            // TODO: ugly. GraphBuilder.AddNodeInternal is processing ports in field declaration order, hence the need here to process variables in a very specific order.
            ProcessVariablesOfGivenTypeAndDirection(vsGraphModel, ModifierFlags.ReadOnly, false, m_PortToOffsetMapping);
            ProcessVariablesOfGivenTypeAndDirection(vsGraphModel, ModifierFlags.WriteOnly, false, m_PortToOffsetMapping);
            ProcessVariablesOfGivenTypeAndDirection(vsGraphModel, ModifierFlags.ReadOnly, true, m_PortToOffsetMapping);
            ProcessVariablesOfGivenTypeAndDirection(vsGraphModel, ModifierFlags.WriteOnly, true, m_PortToOffsetMapping);

            m_Node.Inputs.Port.Index = 2;
            m_Node.Outputs.Port.Index = (uint)(m_Node.Inputs.Port.Index + m_Node.Inputs.DataCount);
            m_Node.DataInputs.Port.Index = (uint)(m_Node.Outputs.Port.Index + m_Node.Outputs.DataCount);
            m_Node.DataOutputs.Port.Index = (uint)(m_Node.DataInputs.Port.Index + m_Node.DataInputs.DataCount);

            void ProcessVariablesOfGivenTypeAndDirection(VSGraphModel graphModel, ModifierFlags expectedFlags,
                bool expectedData, Dictionary<string, uint> mPortToOffsetMapping)
            {
                foreach (var variableModel in graphModel.GraphVariableModels.Where(
                    v => v.IsInputOrOutput()))
                {
                    var isData = !variableModel.IsInputOrOutputTrigger();
                    if (variableModel.Modifiers != expectedFlags || isData != expectedData)
                        continue;
                    PortModel p;
                    if (variableModel.Modifiers == ModifierFlags.ReadOnly)
                    {
                        if (isData)
                        {
                            p = addDataInputPort(variableModel.Name, variableModel.DataType);
                            m_Node.DataInputs.DataCount++;
                        }
                        else
                        {
                            p = addExecutionInputPort(variableModel.Name);
                            m_Node.Inputs.DataCount++;
                        }
                    }
                    else
                    {
                        if (isData)
                        {
                            p = addDataOutputPort(variableModel.Name, variableModel.DataType);
                            m_Node.DataOutputs.DataCount++;
                        }
                        else
                        {
                            p = addExecutionOutputPort(variableModel.Name);
                            m_Node.Outputs.DataCount++;
                        }
                    }

                    mPortToOffsetMapping.Add(p.UniqueId, i++);
                }
            }
        }
    }

    [Serializable]
    public class SmartObjectReferenceNodeModel : VariableNodeModel, IDotsNodeModel, IGraphReferenceNodeModel
    {
        [SerializeField] GraphReferenceData m_GraphReferenceData = new GraphReferenceData();

        public Type NodeType => m_GraphReferenceData.NodeType;
        public INode Node => m_GraphReferenceData.m_Node;
        public Dictionary<string, uint> PortToOffsetMapping => m_GraphReferenceData.m_PortToOffsetMapping;

        public VSGraphAssetModel GraphReference
        {
            get => m_GraphReferenceData.m_GraphReference;
            set
            {
                m_GraphReferenceData.m_GraphReference = value;
                DefineNode();
            }
        }

        public GameObject GetBoundObject(IEditorDataModel editorDataModel)
        {
            if (!editorDataModel?.BoundObject)
                return null;
            var sga = editorDataModel.BoundObject.GetComponent<ScriptingGraphAuthoring>();
            if (!sga)
                return null;
            BindingId id = GraphBuilder.GetBindingId(DeclarationModel);
            return sga.Values.FirstOrDefault(v => v.Id.Equals(id))?.Object as GameObject;
        }

        public override bool HasProgress => m_GraphReferenceData.HasProgress;

        public override string Title => m_GraphReferenceData.m_GraphReference
        ? m_GraphReferenceData.m_GraphReference.Name
        : "<Missing Smart Object>";

        protected override void OnDefineNode()
        {
            if (m_GraphReferenceData == null)
                m_GraphReferenceData = new GraphReferenceData();
            m_GraphReferenceData.OnDefineNode(
                (portName, typeHandle) =>  AddDataInputPort(portName, typeHandle),
                (portName, typeHandle) =>  AddDataOutputPort(portName, typeHandle),
                (portName) => AddExecutionInputPort(portName),
                (portName) => AddExecutionOutputPort(portName));
        }

        public bool NeedsUpdate() => m_GraphReferenceData.NeedsUpdate();
    }
}
