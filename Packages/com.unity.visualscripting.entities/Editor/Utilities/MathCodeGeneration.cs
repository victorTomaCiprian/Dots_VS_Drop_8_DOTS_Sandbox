using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Assert = Unity.Assertions.Assert;

namespace NodeModels
{
    static class MathCodeGeneration
    {
        static string FileName => "MathematicsFunctions.gen.cs";

        static string EnumName => nameof(Runtime.Mathematics.MathGeneratedFunction);
        static string ClassName => "MathGeneratedDelegates";
        static string LastEnumName => "NumMathFunctions";

        static List<string> GenerateEnumNames()
        {
            var opNames = MathOperationsMetaData.SupportedMethods
                .Select(b => b.EnumName)
                .ToList();
            opNames.Add(LastEnumName);
            return opNames;
        }

        static string GetOpCodeFormat(MathOperationsMetaData.OpSignature sig)
        {
            switch (sig.OpType)
            {
                case nameof(MathOperationsMetaData.CustomOps.Negate):
                    return "- {0}";
                case nameof(MathOperationsMetaData.CustomOps.Modulo):
                    return "{0} % {1}";
                case nameof(MathOperationsMetaData.CustomOps.Add):
                    return "{0} + {1}";
                case nameof(MathOperationsMetaData.CustomOps.Subtract):
                    return "{0} - {1}";
                case nameof(MathOperationsMetaData.CustomOps.Multiply):
                    return "{0} * {1}";
                case nameof(MathOperationsMetaData.CustomOps.Divide):
                    return "{0} / {1}";
                case nameof(MathOperationsMetaData.CustomOps.CubicRoot):
                    return "math.pow(math.abs({0}), 1f / 3f)";
            }

            var formatParams = Enumerable.Range(0, sig.Params.Length).Select(i => $"{{{i}}}"); // {0}, {1}, ...
            return $"math.{sig.OpType.ToLower()}({string.Join(", ", formatParams)})"; // something like "math.dot({0}, {1}) or math.cos({0})"
        }

        public static int GetVersion()
        {
            return GenerateDelegateCode().Aggregate(0, (h, s) => h ^ s.GetHashCode());
        }

        static string GetOpCodeGen(MathOperationsMetaData.OpSignature sig)
        {
            if (!sig.SupportsMultiInputs())
            {
                var indexedValues = sig.Params.Select((p, index) => $"values[{index}].{p}").ToArray();
                return $"values => {string.Format(GetOpCodeFormat(sig), indexedValues)}, \t// {sig.EnumName}";
            }

            var aType = sig.Params[0].ToString();
            var bType = sig.Params[1].ToString();
            return "values => \t// " + sig.EnumName + "\n" +
                "\t\t\t{\n" +
                "\t\t\t\tAssert.IsTrue(values.Length >= 2);\n" +
                "\t\t\t\tvar result = values[0];\n" +
                "\t\t\t\tfor (int i = 1; i < values.Length; ++i)\n" +
                $"\t\t\t\t\tresult = {string.Format(GetOpCodeFormat(sig), $"result.{aType}", $"values[i].{bType}")};\n" +
                "\t\t\t\treturn result;\n" +
                "\t\t\t},";
        }

        static List<string> GenerateDelegateCode()
        {
            return MathOperationsMetaData.SupportedMethods.Select(GetOpCodeGen).ToList();
        }

        [MenuItem("internal:Visual Scripting/Generate Math Functions")]
        static void DumpCode()
        {
            if (EditorUtility.DisplayDialog("Invalidate old graphs", "Warning: generating math functions will invalidate all visual scripts containing previous versions of GenericMathNode. Press OK to proceed anyway.", "OK", "Cancel"))
            {
                var filenameFull = Path.Combine(GetFilePathForCodeGen().FullName, FileName);
                var str = GenerateMathFile();
                WriteFile(filenameFull, str.ToString());
                Debug.Log($"wrote CodeGen version {GetVersion()} to {filenameFull}");
            }
        }

        static StringBuilder GenerateMathFile()
        {
            StringBuilder str = new StringBuilder();

            foreach (var line in new[] { "System", "Unity.Mathematics", "UnityEngine", "Assert = Unity.Assertions.Assert" })
            {
                str.Append("using " + line + ";\n");
            }

            str.Append("\n");
            str.Append("namespace Runtime.Mathematics\n");
            str.Append("{\n");
            str.Append("\tpublic enum " + EnumName + "\n");
            str.Append("\t{\n");
            foreach (var line in GenerateEnumNames().Select((l, i) => $"{l} = {i},"))
            {
                str.Append("\t\t" + line + "\n");
            }
            str.Append("\t}\n");
            str.Append("\n");
            str.Append("\tpublic static class " + ClassName + "\n");
            str.Append("\t{\n");
            str.Append("\t\tinternal static int GenerationVersion => " + GetVersion() + ";\n");
            str.Append("\n");
            str.Append("\t\tinternal static MathValueDelegate[] s_Delegates =\n");
            str.Append("\t\t{\n");
            foreach (var delegateCode in GenerateDelegateCode())
            {
                str.Append("\t\t\t" + delegateCode + "\n");
            }
            str.Append("\t\t};\n");
            str.Append("\t}\n");
            str.Append("}\n");

            return str;
        }

        static DirectoryInfo GetFilePathForCodeGen()
        {
            var assetDirPath = Path.GetDirectoryName(Application.dataPath);
            if (assetDirPath == null)
                throw new InvalidOperationException($"Can't get data path : {Application.dataPath}");
            var projectDir = new DirectoryInfo(assetDirPath).Parent;
            if (projectDir == null)
                throw new InvalidOperationException($"Can't get project path : {Application.dataPath}/..");
            string[] fileDirNames = { projectDir.FullName, "Packages", "com.unity.visualscripting.entities", "Runtime", "Interpreter", "Nodes", "Data", "Math" };
            var fileDirName = Path.Combine(fileDirNames);
            var fileDirPath = new DirectoryInfo(fileDirName);
            if (!fileDirPath.Exists)
                throw new InvalidOperationException($"Can't find path to output file: {fileDirPath.FullName}");
            return fileDirPath;
        }

        static void WriteFile(string filename, string text)
        {
            // Convert all tabs to spaces
            text = text.Replace("\t", "    ");
            // Normalize line endings, convert all EOL to platform EOL (and let git handle it)
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\n", Environment.NewLine);

            // Generate auto generated comment
            text = s_AutoGenHeader + text;

            // Trim trailing spaces that could have come from code gen.
            char[] trim = { ' ' };
            var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = lines[i].TrimEnd(trim);
            }

            text = string.Join(Environment.NewLine, lines);

            File.WriteAllText(filename, text);
        }

        static string s_AutoGenHeader =
            "//------------------------------------------------------------------------------\n" +
            "// <auto-generated>\n" +
            "//     This code was generated by a tool.\n" +
            "//\n" +
            "//     Changes to this file may cause incorrect behavior and will be lost if\n" +
            "//     the code is regenerated.\n" +
            "// </auto-generated>\n" +
            "//------------------------------------------------------------------------------\n";
    }
}
