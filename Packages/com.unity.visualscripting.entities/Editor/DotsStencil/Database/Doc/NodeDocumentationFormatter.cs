using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Runtime;
using Runtime.Nodes;
using UnityEditor.Searcher;
using ValueType = Runtime.ValueType;

namespace DotsStencil
{
    internal abstract class NodeDocumentationFormatter
    {
        protected abstract void Paragraph(string paragraph);
        protected abstract void SectionTitle(string title, int titleImportance);
        protected abstract void PortDescription(string portName, string type, string defaultValue, string attrDescription);
        protected abstract void PortsHeader(string sectionName);

        public void DocumentNode(SearcherItem searcherItem, IDotsNodeModel baseDotsNodeModel)
        {
            var title = Attribute.IsDefined(baseDotsNodeModel.Node.GetType(), typeof(WorkInProgressAttribute))
                ? $"{searcherItem.Name} [WIP]"
                : searcherItem.Name;

            SectionTitle(title, 1);
            var nodeDescription = GetNodeDescription(baseDotsNodeModel);
            if (!String.IsNullOrEmpty(nodeDescription))
                Paragraph(nodeDescription);

            GetPortsDescription(baseDotsNodeModel);
        }

        /// <summary>
        /// Generates ports' description in the following format: "Name [Type]: Description (Default value)"
        /// </summary>
        /// <param name="nodeModel">The node holding the ports to describe</param>
        /// <returns>The description of all the ports in the node</returns>
        private void GetPortsDescription(IDotsNodeModel nodeModel)
        {
            var runtimeNode = nodeModel.Node;
            var nodeType = runtimeNode.GetType();
            var fields = nodeType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var inputs = fields.Where(f => typeof(IInputDataPort).IsAssignableFrom(f.FieldType)
                || typeof(IInputTriggerPort).IsAssignableFrom(f.FieldType)).ToList();
            var outputs = fields.Where(f => typeof(IOutputDataPort).IsAssignableFrom(f.FieldType)
                || typeof(IOutputTriggerPort).IsAssignableFrom(f.FieldType)).ToList();

            SectionTitle("Ports", 2);

            AppendSection("Inputs", inputs, this);
            AppendSection("Outputs", outputs, this);
        }

        private static string GetNodeDescription(IDotsNodeModel nodeModel)
        {
            var runtimeNode = nodeModel.Node;
            var nodeType = runtimeNode.GetType();

            if (nodeType.GetInterfaces().Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHasExecutionType<>)))
            {
                var getType = nodeType.GetProperty("Type")?.GetMethod;
                var value = getType?.Invoke(runtimeNode, new object[] {});
                var attrs = nodeType.GetCustomAttributes<NodeDescriptionAttribute>();
                return attrs.FirstOrDefault(a => a.Type.Equals(value))?.Description;
            }

            return nodeType.GetCustomAttribute<NodeDescriptionAttribute>()?.Description;
        }

        private static void AppendSection(string sectionName, IReadOnlyCollection<FieldInfo> infos, NodeDocumentationFormatter formatter)
        {
            if (infos.Any())
            {
                formatter.PortsHeader(sectionName);

                foreach (var info in infos)
                {
                    var attr = info.GetCustomAttribute<PortDescriptionAttribute>();
                    if (attr == null)
                        continue;

                    var portName = String.IsNullOrEmpty(attr.Name) ? info.Name : attr.Name;
                    var type = typeof(ITriggerPort).IsAssignableFrom(info.FieldType) ? "Trigger" : attr.Type != ValueType.Unknown ? $"[{attr.Type.FriendlyName()}]" : String.Empty;
                    var defaultValue = attr.DefaultValue?.ToString();
                    formatter.PortDescription(portName, type, defaultValue, attr.Description);
                }
            }
        }
    }
}
