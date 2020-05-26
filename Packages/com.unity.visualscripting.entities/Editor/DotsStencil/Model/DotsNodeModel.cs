using System;
using System.Collections.Generic;
using System.Reflection;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.Assertions;
using PortType = Runtime.PortType;
using ValueType = Runtime.ValueType;

namespace DotsStencil
{
    /// <summary>
    /// Quickly define a node model from a runtime node by subclassing this. It owns (and serializes) an instance of the
    /// runtime node that will be used during translation. However that instance's port indices will be remapped during
    /// translation.
    /// Having to define them one by one means we
    /// could keep the same model class with a different base class in the future if we realize it won't be a 1 to 1
    /// mapping (eg. a Mul node model might output a MulInt, MulMatrix, ... runtime node)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class DotsNodeModel<T> : BaseDotsNodeModel, IDotsNodeModel, IPropertyVisitorNodeTarget where T : struct, INode
    {
        static readonly List<PortMetaData> k_DefaultPortList = new List<PortMetaData>(1) { default };

        public override bool HasProgress => m_Node is INodeReportProgress;
        public Type NodeType => typeof(T);
        public T TypedNode => (T)Node;
        public override string Title => NodeType.Name;
        public Dictionary<string, uint> PortToOffsetMapping { get; private set; }

        public DotsNodeModel()
        {
            var t = NodeType;
            if (t.GetCustomAttribute<SerializableAttribute>() == null)
                Debug.LogError($"Node type \"{t}\" does not have the Serializable attribute");
            if (ReferenceEquals(null, m_Node))
                m_Node = (INode)Activator.CreateInstance(t);
        }

        protected override void OnDefineNode()
        {
            if (PortToOffsetMapping != null)
                PortToOffsetMapping.Clear();
            else
                PortToOffsetMapping = new Dictionary<string, uint>();

            var ctx = new TranslationSetupContext();

            var portCustomData = PortCustomData; // cache this in case the implementation is a dynamic getter
            foreach (var fieldInfo in GetNodePorts(NodeType))
            {
                var port = (IPort)fieldInfo.GetValue(m_Node);
                var portMetadata = DefaultPortDescription(fieldInfo);
                if (portCustomData.ContainsKey(fieldInfo.Name))
                {
                    portMetadata = portCustomData[fieldInfo.Name];
                    if (port is IMultiDataPort multiDataPort)
                    {
                        multiDataPort.SetCount(portMetadata.Count);
                        fieldInfo.SetValue(m_Node, multiDataPort);
                    }
                }

                // assume this is the only node in the world. we patch the actual indices later in the translator. at
                // this point, port indices will start from 1 (0 being an invalid port)
                port = ctx.SetupPort(m_Node, fieldInfo, out var direction, out var type, out string _);
                for (int i = 0; i < portMetadata.Count; i++)
                {
                    IPortModel portModel;
                    var portName = portMetadata[i].Name;
                    var portId = portName != "" ? portName : $"{fieldInfo.Name}_{i}";
                    var portType = portMetadata[i].Type.ValueTypeToTypeHandle();
                    var portModelOptions = portMetadata[i].PortModelOptions;

                    switch (port)
                    {
                        // TODO: Fill in the ports for the IHasMainTriggerInputPort, IHasMainTriggerOutputPort, IHasMainInputPort, IHasMainOutputPort interfaces
                        case IInputDataPort _:
                        {
                            var j = i;
                            void PreDefine(ConstantNodeModel model)
                            {
                                var modelObjectValue = portMetadata[j].DefaultValue;
                                if (modelObjectValue != null)
                                {
                                    model.ObjectValue = modelObjectValue;
                                }
                            }

                            portModel = AddDataInputPort(portName, portType, portId, preDefine: PreDefine, options: portModelOptions);
                            if (this is IHasMainInputPort hasPort && hasPort.InputPort == null)
                            {
                                var f = GetType().GetProperty(nameof(IHasMainInputPort.InputPort));
                                Assert.IsNotNull(f, $"{GetType()}.InputPort must have a set property.");
                                f.SetValue(this, portModel);
                            }

                            break;
                        }

                        case IOutputDataPort _:
                        {
                            portModel = AddDataOutputPort(portName, portType, portId);
                            if (this is IHasMainOutputPort hasPort && hasPort.OutputPort == null)
                            {
                                var f = GetType().GetProperty(nameof(IHasMainOutputPort.OutputPort));
                                Assert.IsNotNull(f, $"{GetType()}.OutputPort must have a set property.");
                                f.SetValue(this, portModel);
                            }

                            break;
                        }

                        default:
                            switch (direction)
                            {
                                case PortDirection.Input when type == PortType.Trigger:
                                {
                                    portModel = AddExecutionInputPort(portName, portId);
                                    if (this is IHasMainExecutionInputPort hasPort && hasPort.ExecutionInputPort == null)
                                    {
                                        var f = GetType().GetProperty(nameof(IHasMainExecutionInputPort.ExecutionInputPort));
                                        Assert.IsNotNull(f, $"{GetType()}.ExecutionInputPort must have a set property.");
                                        f.SetValue(this, portModel);
                                    }

                                    break;
                                }

                                case PortDirection.Output when type == PortType.Trigger:
                                {
                                    portModel = AddExecutionOutputPort(portName, portId);
                                    if (this is IHasMainExecutionOutputPort hasPort && hasPort.ExecutionOutputPort == null)
                                    {
                                        var f = GetType().GetProperty(nameof(IHasMainExecutionOutputPort.ExecutionOutputPort));
                                        Assert.IsNotNull(f, $"{GetType()}.ExecutionOutputPort must have a set property.");
                                        f.SetValue(this, portModel);
                                    }

                                    break;
                                }
                                default:
                                    throw new NotImplementedException();
                            }

                            break;
                    }

                    Assert.IsNotNull(portModel);
                    PortToOffsetMapping.Add(portModel.UniqueId, port.GetPort().Index + (uint)i);
                }
            }
        }

        static List<PortMetaData> DefaultPortDescription(FieldInfo portField)
        {
            k_DefaultPortList[0] = GetPortMetadata(portField);
            return k_DefaultPortList;
        }

        public static PortMetaData GetPortMetadata(string portName)
        {
            return GetPortMetadata(typeof(T).GetField(portName));
        }

        // IPropertyVisitorNodeTarget implementation

        public object Target
        {
            get => m_Node;
            set => m_Node = (INode)value;
        }

        public bool IsExcluded(object value)
        {
            return value is IPort;
        }
    }
}
