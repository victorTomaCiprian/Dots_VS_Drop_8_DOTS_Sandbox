using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Runtime.Mathematics;
using Unity.Mathematics;
using UnityEditor.EditorCommon.Extensions;
using UnityEngine;
using ValueType = Runtime.ValueType;

namespace NodeModels
{
    public static class MathOperationsMetaData
    {
        public enum CustomOps
        {
            Add, Subtract, Multiply, Divide, Negate, Modulo, CubicRoot
        }

        public enum MathOps
        {
            Sin, Cos, Tan, Sinh, Cosh, Tanh, Asin, Acos, Atan, Atan2, Round, Ceil, Floor, Abs, Exp, Log10, Log2, Sign, Sqrt, Dot, Cross, Pow, Min, Max,
        }

        static string[] s_MethodNamesWithMultipleInputs =
        {
            "add", "multiply", "min", "max"
        };

        public static bool MethodNameSupportsMultipleInputs(string methodName)
        {
            var m = methodName.ToLower();
            return s_MethodNamesWithMultipleInputs.Contains(m);
        }

        public struct OpSignature
        {
            public ValueType Return;
            public ValueType[] Params;
            public string OpType;

            public string EnumName => OpType + string.Join("", Params);

            public OpSignature(ValueType @return, CustomOps opType, params ValueType[] @params)
                : this(@return, opType.ToString(), @params)
            {
            }

            public OpSignature(ValueType @return, string opType, params ValueType[] @params)
            {
                Params = @params;
                Return = @return;
                OpType = opType;
            }

            public static OpSignature LinearBinOp(CustomOps opType, ValueType valueType)
            {
                return new OpSignature(valueType, opType, valueType, valueType);
            }

            public override string ToString()
            {
                return $"{Return} {OpType}({string.Join(", ", Params.Select(p => p.ToString()))}) ({EnumName})";
            }

            public bool SupportsMultiInputs() => MethodNameSupportsMultipleInputs(OpType);
        }

        static List<OpSignature> s_SupportedMethods;

        static List<OpSignature> s_SupportedMathMethods;

        static List<OpSignature> s_SupportedCustomMethods;

        static Dictionary<string, OpSignature[]> s_MethodsByName;

        static Dictionary<OpSignature, MathGeneratedFunction> s_EnumForSignature;
        static Dictionary<MathGeneratedFunction, OpSignature> s_SignatureForEnum;

        public static IReadOnlyList<OpSignature> SupportedMethods => s_SupportedMethods ?? (s_SupportedMethods = GetSupportedMethods());

        public static IReadOnlyList<OpSignature> SupportedMathMethods => s_SupportedMathMethods ?? (s_SupportedMathMethods = GetMathMethods());

        public static IReadOnlyList<OpSignature> SupportedCustomMethods => s_SupportedCustomMethods ?? (s_SupportedCustomMethods = GetCustomMethods());
        public static IReadOnlyDictionary<string, OpSignature[]> MethodsByName => s_MethodsByName ?? (s_MethodsByName = GetMethodsByName());
        public static IReadOnlyDictionary<OpSignature, MathGeneratedFunction> EnumForSignature => s_EnumForSignature ?? (s_EnumForSignature = GetEnumsForSignatures());
        public static IReadOnlyDictionary<MathGeneratedFunction, OpSignature> SignatureForEnum => s_SignatureForEnum ?? (s_SignatureForEnum = GetSignatureForEnum());

        static Dictionary<string, OpSignature[]> GetMethodsByName()
        {
            var opsByName = new Dictionary<string, List<OpSignature>>();
            foreach (var signature in SupportedMethods)
            {
                if (!opsByName.ContainsKey(signature.OpType))
                    opsByName.Add(signature.OpType, new List<OpSignature>());
                opsByName[signature.OpType].Add(signature);
            }

            foreach (var list in opsByName.Values)
            {
                ArrangeFloatParamsFirst(list);
            }

            return opsByName.ToDictionary(kp => kp.Key, kp => kp.Value.ToArray());
        }

        static void ArrangeFloatParamsFirst(List<OpSignature> list)
        {
            for (var i = 1; i < list.Count; i++)
            {
                var signature = list[i];
                if (!signature.Params.All(p => p == ValueType.Float))
                    continue;
                if (i == 0)
                    return;
                var tmp = list[0];
                list[0] = list[i];
                list[i] = tmp;
                return;
            }
        }

        static List<OpSignature> GetSupportedMethods()
        {
            return SupportedMathMethods.Concat(SupportedCustomMethods).ToList();
        }

        static List<OpSignature> GetMathMethods()
        {
            var funcNames = Enum.GetNames(typeof(MathOps)).Select(s => s.ToLower()).ToHashSet();
            var mathMethods = typeof(math).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var res = new List<OpSignature>(mathMethods.Length);
            foreach (var m in mathMethods)
            {
                if (funcNames.Contains(m.Name))
                {
                    ValueType returnType = TypeToValueType(m.ReturnType);
                    if (returnType != ValueType.Unknown)
                    {
                        var paramTypes = m.GetParameters().Select(p => TypeToValueType(p.ParameterType)).ToArray();
                        if (paramTypes.All(p => p != ValueType.Unknown))
                        {
                            res.Add(new OpSignature(returnType, m.Name.Capitalize(), paramTypes));
                        }
                    }
                }
            }
            return res;
        }

        static List<OpSignature> GetCustomMethods()
        {
            var res = new List<OpSignature>
            {
                new OpSignature(ValueType.Float, CustomOps.Add, ValueType.Int, ValueType.Float),
                new OpSignature(ValueType.Float, CustomOps.Add, ValueType.Float, ValueType.Int),
                new OpSignature(ValueType.Float, CustomOps.Subtract, ValueType.Int, ValueType.Float),
                new OpSignature(ValueType.Float, CustomOps.Subtract, ValueType.Float, ValueType.Int),
                new OpSignature(ValueType.Float, CustomOps.Multiply, ValueType.Float, ValueType.Int),
                new OpSignature(ValueType.Float, CustomOps.Multiply, ValueType.Int, ValueType.Float),
                new OpSignature(ValueType.Float2, CustomOps.Multiply, ValueType.Float2, ValueType.Float),
                new OpSignature(ValueType.Float2, CustomOps.Multiply, ValueType.Float, ValueType.Float2),
                new OpSignature(ValueType.Float3, CustomOps.Multiply, ValueType.Float3, ValueType.Float),
                new OpSignature(ValueType.Float3, CustomOps.Multiply, ValueType.Float, ValueType.Float3),
                new OpSignature(ValueType.Float4, CustomOps.Multiply, ValueType.Float4, ValueType.Float),
                new OpSignature(ValueType.Float4, CustomOps.Multiply, ValueType.Float, ValueType.Float4),
            };

            // types for which Op(T, T) is typed T, e.g. int add(int, int)
            var regularBinOps = new Dictionary<CustomOps, ValueType[]>
            {
                { CustomOps.Add, new[] { ValueType.Int, ValueType.Float, ValueType.Float2, ValueType.Float3, ValueType.Float4 } },
                { CustomOps.Subtract, new[] { ValueType.Int, ValueType.Float, ValueType.Float2, ValueType.Float3, ValueType.Float4 } },
                { CustomOps.Divide, new[] { ValueType.Int, ValueType.Float } },
                { CustomOps.Multiply, new[] { ValueType.Int, ValueType.Float } },
                { CustomOps.Modulo, new[] { ValueType.Int } },
            };

            foreach (var kp in regularBinOps)
            {
                foreach (var valueType in kp.Value)
                {
                    res.Add(OpSignature.LinearBinOp(kp.Key, valueType));
                }
            }

            var regularUnaryOps = new Dictionary<CustomOps, ValueType[]>
            {
                { CustomOps.Negate, new[] { ValueType.Float, ValueType.Float2, ValueType.Float3, ValueType.Float4, ValueType.Int } },
                { CustomOps.CubicRoot, new[] { ValueType.Float } },
            };

            foreach (var kp in regularUnaryOps)
            {
                foreach (var valueType in kp.Value)
                {
                    res.Add(new OpSignature(valueType, kp.Key, valueType));
                }
            }

            return res;
        }

        static Dictionary<OpSignature, MathGeneratedFunction> GetEnumsForSignatures()
        {
            return SupportedMethods.ToDictionary(o => o, o => (MathGeneratedFunction)Enum.Parse(typeof(MathGeneratedFunction), o.EnumName));
        }

        static Dictionary<MathGeneratedFunction, OpSignature> GetSignatureForEnum()
        {
            return SupportedMethods.ToDictionary(o => (MathGeneratedFunction)Enum.Parse(typeof(MathGeneratedFunction), o.EnumName), o => o);
        }

        public static OpSignature GetMethodsSignature(this MathGeneratedFunction function)
        {
            return SignatureForEnum[function];
        }

        static ValueType TypeToValueType(Type t)
        {
            if (t == typeof(int))
                return ValueType.Int;
            if (t == typeof(bool))
                return ValueType.Bool;
            if (t == typeof(float))
                return ValueType.Float;
            if (t == typeof(float2))
                return ValueType.Float2;
            if (t == typeof(float3))
                return ValueType.Float3;
            if (t == typeof(float4))
                return ValueType.Float4;
            return ValueType.Unknown;
        }

        // Darren https://stackoverflow.com/a/27073919
        static string Capitalize(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}
