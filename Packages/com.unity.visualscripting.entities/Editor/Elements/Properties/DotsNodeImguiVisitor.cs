using System;
using System.Collections.Generic;
using System.Linq;
using DotsStencil;
using NodeModels;
using Runtime;
using Runtime.Mathematics;
using Unity.Entities;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using Unity.Properties;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Runtime;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    class ULongTypeHandleAdapter : ImguiVisitorBase.TypeHandleSearcherAdapter, IVisitAdapter<TypeReference>
    {
        static Dictionary<Tuple<INodeModel, string>, TypeHandle> s_CachedTypeHandle;
        static Dictionary<Tuple<INodeModel, string>, TypeHandle> SafeTypeCache => s_CachedTypeHandle ?? (s_CachedTypeHandle = new Dictionary<Tuple<INodeModel, string>, TypeHandle>());
        public ULongTypeHandleAdapter(ImguiVisitorBase highLevelNodeImguiVisitor)
            : base(highLevelNodeImguiVisitor) {}

        public VisitStatus Visit<TProperty, TContainer>(IPropertyVisitor visitor, TProperty property, ref TContainer container, ref TypeReference value, ref ChangeTracker changeTracker) where TProperty : IProperty<TContainer, TypeReference>
        {
            if (!property.Attributes.HasAttribute<TypeSearcherAttribute>())
                return VisitStatus.Unhandled;

            var key = new Tuple<INodeModel, string>(VisitorModel, ((IProperty)property).GetName());
            if (!SafeTypeCache.TryGetValue(key, out var previousType))
            {
                int typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(value.TypeHash);
                if (typeIndex != -1 && typeIndex != 0)
                {
                    previousType = TypeManager.GetType(typeIndex).GenerateTypeHandle(Stencil);
                    SafeTypeCache[key] = previousType;
                }
                else
                    previousType = default;
            }

            if (ExposeProperty(property, ref changeTracker, ref previousType))
            {
                SafeTypeCache[key] = previousType;
                value.TypeHash = TypeHash.CalculateStableTypeHash(previousType.Resolve(Stencil));
            }

            return VisitStatus.Handled;
        }

        protected override SearcherFilter MakeSearcherAdapterForProperty(IProperty property, INodeModel model)
        {
            var stencil = model.GraphModel.Stencil;
            var typeSearcher = new SearcherFilter(SearcherContext.Type);
            var attribute = property.Attributes.GetAttribute<TypeSearcherAttribute>();
            if (attribute is ComponentSearcherAttribute componentSearcher)
                return componentSearcher.ComponentOptions == ComponentOptions.OnlyAuthoringComponents
                    ? typeSearcher.WithAuthoringComponentTypes(stencil)
                    : typeSearcher.WithComponentTypes(stencil);
            return typeSearcher.WithTypesInheriting(stencil, attribute.FilteredType);
        }
    }

    class MathGeneratedFunctionAdapter : ImguiVisitorBase.PickFromSearcherAdapter<MathGeneratedFunction>, IVisitAdapter<MathGeneratedFunction>
    {
        protected override MathGeneratedFunction InvalidValue => MathGeneratedFunction.NumMathFunctions;

        public MathGeneratedFunctionAdapter(ImguiVisitorBase highLevelNodeImguiVisitor)
            : base(highLevelNodeImguiVisitor)
        {
            m_Picked = MathGeneratedFunction.NumMathFunctions;
        }

        public VisitStatus Visit<TProperty, TContainer>(IPropertyVisitor visitor, TProperty property, ref TContainer container, ref MathGeneratedFunction value, ref ChangeTracker changeTracker) where TProperty : IProperty<TContainer, MathGeneratedFunction>
        {
            ExposeProperty(property, ref changeTracker, ref value);
            return VisitStatus.Handled;
        }

        protected override string Title(MathGeneratedFunction function)
        {
            return OpTitle(function.GetMethodsSignature());
        }

        static string OpTitle(MathOperationsMetaData.OpSignature op)
        {
            return $"{op.OpType}({string.Join(", ", op.Params.Select(p => p.ToString()))})";
        }

        protected override void ShowSearcher(Stencil stencil, Vector2 position, IProperty property, MathGeneratedFunction currentValue)
        {
            var currentMethodName = currentValue.GetMethodsSignature().OpType;
            var opSignatures = MathOperationsMetaData.MethodsByName[currentMethodName];

            void OnValuePicked(string s, int i)
            {
                if (MathOperationsMetaData.EnumForSignature.TryGetValue(opSignatures[i], out var value))
                    m_Picked = value;
            }

            SearcherService.ShowValues("Types", opSignatures.Select(OpTitle), position, OnValuePicked);
        }
    }

    class DotsNodeImguiVisitor : HighLevelNodeImguiVisitor
    {
        protected override IEnumerable<IPropertyVisitorAdapter> Adapters =>
            new IPropertyVisitorAdapter[]
        {
            new ULongTypeHandleAdapter(this),
            new MathGeneratedFunctionAdapter(this)
        }.Concat(base.Adapters);
    }
}
