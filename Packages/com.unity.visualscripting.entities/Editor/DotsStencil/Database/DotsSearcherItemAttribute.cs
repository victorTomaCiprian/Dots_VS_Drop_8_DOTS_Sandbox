using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NodeModels;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using Unity.Entities;
using UnityEditor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using  UnityEditor.VisualScripting.Model;

namespace DotsStencil
{
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public class DotsSearcherItemAttribute : SearcherItemAttribute
    {
        public DotsSearcherItemAttribute(string path)
            : base(typeof(DotsStencil), SearcherContext.Graph, path) {}
    }

    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class DotsSearcherItemCollectionAttribute : Attribute
    {
        public string Path { get; }
        public string NameFormat { get; }

        public struct TypeObjectData
        {
            public string SearcherTitle;
            public object Value;
        }

        public abstract IEnumerable<TypeObjectData> ObjectData { get; }

        protected DotsSearcherItemCollectionAttribute(string path, string nameFormat = "")
        {
            Path = path;
            NameFormat = nameFormat;
        }
    }

    public class EnumNodeSearcherAttribute : DotsSearcherItemCollectionAttribute
    {
        public Type EnumType { get; }

        public EnumNodeSearcherAttribute(Type enumType, string path, string format = "")
            : base(path, format)
        {
            Assert.IsTrue(enumType.IsEnum, "The collection type must be an Enum type");
            EnumType = enumType;
        }

        public override IEnumerable<TypeObjectData> ObjectData
        {
            get
            {
                var objects = Enum.GetValues(EnumType).Cast<Enum>();
                return objects.Select(o => new TypeObjectData { Value = o, SearcherTitle = o.ToString().Nicify() });
            }
        }
    }

    public class GeneratedMathSearcherAttribute : DotsSearcherItemCollectionAttribute
    {
        public GeneratedMathSearcherAttribute(string path, string format = "")
            : base(path, format) {}

        public override IEnumerable<TypeObjectData> ObjectData
        {
            get
            {
                return MathOperationsMetaData.MethodsByName.Keys.Select(s =>
                {
                    var firstSig = MathOperationsMetaData.MethodsByName[s].First();
                    return new TypeObjectData { Value = FindFunction(firstSig), SearcherTitle = firstSig.OpType };
                });
            }
        }

        static MathGeneratedFunction FindFunction(MathOperationsMetaData.OpSignature sig)
        {
            if (Enum.TryParse<MathGeneratedFunction>(sig.EnumName, out var res))
                return res;
            return MathGeneratedFunction.NumMathFunctions;
        }
    }

    public class ComponentNodeSearcherAttribute : DotsSearcherItemCollectionAttribute
    {
        static List<TypeObjectData> s_ObjectData;
        public ComponentNodeSearcherAttribute(string componentActionName)
            : base(componentActionName + " Component", componentActionName + " {0}")
        {
        }

        public override IEnumerable<TypeObjectData> ObjectData => s_ObjectData ?? (s_ObjectData = BuildObjectData());

        static List<TypeObjectData> BuildObjectData()
        {
            var types = TypeCache.GetTypesWithAttribute<GenerateAuthoringComponentAttribute>();
            return types
                .Select(t => new TypeObjectData { Value = new TypeReference {TypeHash = TypeHash.CalculateStableTypeHash(t) }, SearcherTitle = t.Name })
                .ToList();
        }
    }
}
