using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Runtime;
using Runtime.Nodes;
using UnityEditor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VisualScripting;

namespace DotsStencil
{
    static class GraphElementSearcherDatabaseExtensions
    {
        internal static GraphElementSearcherDatabase AddDotsEvents(this GraphElementSearcherDatabase self)
        {
            var eventTypes = TypeCache.GetTypesDerivedFrom<IDotsEvent>()
                .Where(t => !Attribute.IsDefined(t, typeof(HiddenAttribute)));
            var sendEventNodeType = typeof(SendEventNodeModel);
            var onEventNodeType = typeof(OnEventNodeModel);

            foreach (var eventType in eventTypes)
            {
                var typeHandle = eventType.GenerateTypeHandle(self.Stencil);

                self.Items.AddAtPath(new GraphNodeModelSearcherItem(
                    new NodeSearcherItemData(sendEventNodeType),
                    data => data.CreateNode(
                        sendEventNodeType,
                        preDefineSetup: n => ((IEventNodeModel)n).TypeHandle = typeHandle),
                    $"Send {eventType.FriendlyName()}"),
                    "Events");

                self.Items.AddAtPath(new GraphNodeModelSearcherItem(
                    new NodeSearcherItemData(onEventNodeType),
                    data => data.CreateNode(
                        onEventNodeType,
                        preDefineSetup: n => ((IEventNodeModel)n).TypeHandle = typeHandle),
                    $"On {eventType.FriendlyName()}"),
                    "Events");
            }

            return self;
        }

        internal static GraphElementSearcherDatabase AddDotsConstants(this GraphElementSearcherDatabase self)
        {
            var constants = new Dictionary<string, Type>
            {
                { "Boolean Constant", typeof(BooleanConstantNodeModel) },
                { "Integer Constant", typeof(IntConstantModel) },
                { "Float Constant", typeof(FloatConstantModel) },
                { "Vector 2 Constant", typeof(Vector2ConstantModel) },
                { "Vector 3 Constant", typeof(Vector3ConstantModel) },
                { "Vector 4 Constant", typeof(Vector4ConstantModel) },
                { "Quaternion Constant", typeof(QuaternionConstantModel) },
                { "Object Constant", typeof(ObjectConstantModel) },
                { "String Constant", typeof(StringConstantModel) },
            };

            foreach (var constant in constants)
            {
                self.Items.AddAtPath(new GraphNodeModelSearcherItem(
                    new NodeSearcherItemData(constant.Value),
                    data => data.CreateNode(constant.Value),
                    constant.Key),
                    "Constants");
            }

            return self;
        }

        // TODO temp while developing. Will not be created from searcher in the long run.
        internal static GraphElementSearcherDatabase AddEdgePortals(this GraphElementSearcherDatabase self)
        {
            var portals = new Dictionary<string, Type>
            {
                { "Data Portal Entry", typeof(DataEdgePortalEntryModel) },
                { "Trigger Portal Entry", typeof(ExecutionEdgePortalEntryModel) },
                { "Data Portal Exit", typeof(DataEdgePortalExitModel) },
                { "Trigger Portal Exit", typeof(ExecutionEdgePortalExitModel) },
            };

            foreach (var portal in portals)
            {
                self.Items.AddAtPath(new GraphNodeModelSearcherItem(
                    new NodeSearcherItemData(portal.Value),
                    data => data.CreateNode(portal.Value),
                    portal.Key),
                    "Portals");
            }

            return self;
        }

        internal static GraphElementSearcherDatabase AddNodesWithSearcherItemCollectionAttribute(
            this GraphElementSearcherDatabase self)
        {
            var types = TypeCache.GetTypesWithAttribute<DotsSearcherItemCollectionAttribute>();
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<DotsSearcherItemCollectionAttribute>();
                Assert.IsTrue(typeof(BaseDotsNodeModel).IsAssignableFrom(type));
                foreach (var objectData in attribute.ObjectData)
                {
                    var name = string.IsNullOrEmpty(attribute.NameFormat)
                        ? objectData.SearcherTitle
                        : string.Format(attribute.NameFormat, objectData.SearcherTitle);

                    self.Items.AddAtPath(new GraphNodeModelSearcherItem(
                        new NodeSearcherItemData(type),
                        data => data.CreateNode(
                            type,
                            preDefineSetup: n =>
                            {
                                var baseDotsNodeModel = (BaseDotsNodeModel)n;
                                var runtimeNode = baseDotsNodeModel.Node;

                                if (runtimeNode == null || !runtimeNode.GetType().GetInterfaces().Any(x =>
                                    x.IsGenericType &&
                                    x.GetGenericTypeDefinition() == typeof(IHasExecutionType<>)))
                                    return;

                                var setNode = runtimeNode.GetType().GetProperty("Type")?.SetMethod;
                                setNode?.Invoke(runtimeNode, new[] { objectData.Value });

                                baseDotsNodeModel.Node = runtimeNode;
                            }),
                        name),
                        attribute.Path);
                }
            }

            return self;
        }
    }
}
