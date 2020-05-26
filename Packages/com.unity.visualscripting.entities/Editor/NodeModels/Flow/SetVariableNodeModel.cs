using System;
using System.Collections.Generic;
using Runtime;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using ValueType = Runtime.ValueType;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Set Var")]
    class SetVariableNodeModel : VariableNodeModel, IDotsNodeModel
    {
        [SerializeField]
        bool m_IsGetter;
        Dictionary<string, uint> m_PortToOffsetMapping;
        SetVariable m_Node;
        public Type NodeType => typeof(SetVariable);

        public INode Node => m_Node;

        public Dictionary<string, uint> PortToOffsetMapping => m_PortToOffsetMapping;

        public bool IsGetter
        {
            get => m_IsGetter;
            set => m_IsGetter = value;
        }

        public override void UpdateTypeFromDeclaration()
        {
            base.UpdateTypeFromDeclaration();
            m_Node.VariableType = DeclarationModel.DataType.TypeHandleToValueTypeOrUnknown();
        }

        protected override void OnDefineNode()
        {
            if (m_PortToOffsetMapping == null)
                m_PortToOffsetMapping = new Dictionary<string, uint>();
            else
                m_PortToOffsetMapping.Clear();
            var triggerSet = AddExecutionInputPort("", "Set");
            var triggerDone = AddExecutionOutputPort("", "Done");
            var dataSet = AddDataInputPort("", DeclarationModel?.DataType ?? TypeHandle.Unknown, "setvalue");

            DotsTranslator.MapPort(m_PortToOffsetMapping, triggerSet.UniqueId, ref m_Node.Input.Port, m_Node);
            DotsTranslator.MapPort(m_PortToOffsetMapping, triggerDone.UniqueId, ref m_Node.Output.Port, m_Node);
            DotsTranslator.MapPort(m_PortToOffsetMapping, dataSet.UniqueId, ref m_Node.Value.Port, m_Node);
            if (m_IsGetter)
            {
                var dataGet = m_MainPortModel = AddDataOutputPort("Value", DeclarationModel?.DataType ?? TypeHandle.Unknown, "getvalue");
                DotsTranslator.MapPort(m_PortToOffsetMapping, dataGet.UniqueId, ref m_Node.OutValue.Port, m_Node);
            }

            m_Node.VariableType = DeclarationModel?.DataType.TypeHandleToValueTypeOrUnknown() ?? ValueType.Unknown;
        }
    }
}
