using System;
using System.Linq;
using Runtime;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using ValueType = Runtime.ValueType;

namespace DotsStencil
{
    static class DotsTypeSearcherDatabaseExtensions
    {
        internal static TypeSearcherDatabase AddBasicDotsTypes(this TypeSearcherDatabase self)
        {
            self.RegisterTypes(items => items.AddRange(
                Enum.GetValues(typeof(ValueType))
                    .Cast<ValueType>()
                    .Select(x => (SearcherItem) new TypeSearcherItem(x == ValueType.Entity ? TypeHandle.GameObject : x.ValueTypeToTypeHandle(), x.FriendlyName()))));
            return self;
        }

        internal static TypeSearcherDatabase AddTypesInheritingFrom<T>(this TypeSearcherDatabase self)
        {
            var baseType = typeof(T);
            self.RegisterTypesFromMetadata((items, metadata) =>
            {
                if (!metadata.IsAssignableTo(baseType))
                    return false;
                var classItem = new TypeSearcherItem(metadata.TypeHandle, metadata.FriendlyName);
                return items.TryAddClassItem(self.Stencil, classItem, metadata);
            });
            return self;
        }
    }
}
