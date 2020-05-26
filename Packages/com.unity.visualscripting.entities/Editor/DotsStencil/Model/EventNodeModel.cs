using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace DotsStencil
{
    [UsedImplicitly]
    public interface IEventNodeModel : IDotsNodeModel, IPropertyVisitorNodeTarget
    {
        TypeHandle TypeHandle { get; set; }
    }

    // ReSharper disable once InconsistentNaming
    static class IEventNodeModelExtensions
    {
        public static IEnumerable<BaseDotsNodeModel.PortMetaData> GetPortsMetaData(
            this IEventNodeModel self,
            Stencil stencil)
        {
            var type = self.TypeHandle.Resolve(stencil);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                yield return new BaseDotsNodeModel.PortMetaData
                {
                    Name = field.Name,
                    Type = field.FieldType.GenerateTypeHandle(stencil).TypeHandleToValueType()
                };
            }
        }
    }

    [Serializable]
    class SendEventNodeModel : DotsNodeModel<SendEvent>, IEventNodeModel, IHasMainExecutionInputPort,
        IHasMainExecutionOutputPort
    {
        [SerializeField]
        TypeHandle m_TypeHandle;

        public TypeHandle TypeHandle
        {
            get => m_TypeHandle;
            set => m_TypeHandle = value;
        }

        public override string Title => $"Send {TypeHandle.Name(Stencil)}";
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }

        public override IReadOnlyDictionary<string, List<PortMetaData>> PortCustomData =>
            new Dictionary<string, List<PortMetaData>>
        {
            { nameof(SendEvent.Values), new List<PortMetaData>(this.GetPortsMetaData(Stencil)) },
        };
    }

    [Serializable]
    class OnEventNodeModel : DotsNodeModel<OnEvent>, IEventNodeModel, IHasMainExecutionOutputPort
    {
        [SerializeField]
        TypeHandle m_TypeHandle;

        public TypeHandle TypeHandle
        {
            get => m_TypeHandle;
            set => m_TypeHandle = value;
        }

        public override string Title => $"On {TypeHandle.Name(Stencil)}";
        public IPortModel ExecutionOutputPort { get; set; }

        public override IReadOnlyDictionary<string, List<PortMetaData>> PortCustomData =>
            new Dictionary<string, List<PortMetaData>>
        {
            { nameof(OnEvent.Values), new List<PortMetaData>(this.GetPortsMetaData(Stencil)) },
        };
    }
}
