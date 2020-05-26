using System;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.Assertions;

namespace DotsStencil
{
    public static class DotsModelExtensions
    {
        public static bool IsObjectReference(this IVariableDeclarationModel declarationModel) => declarationModel is VariableDeclarationModel d && (d.variableFlags & VariableFlags.Hidden) != 0;
        public static bool IsSmartObject(this IVariableDeclarationModel declarationModel) => declarationModel.Modifiers == ModifierFlags.ReadWrite;

        public static void MakeSmartObject(this IVariableDeclarationModel declarationModel)
        {
            ((VariableDeclarationModel)declarationModel).Modifiers = ModifierFlags.ReadWrite;
        }

        public static void MakeObjectReference(this IVariableDeclarationModel declarationModel)
        {
            ((VariableDeclarationModel)declarationModel).variableFlags |= VariableFlags.Hidden;
        }

        public static bool IsInputOrOutput(this IVariableDeclarationModel declarationModel) => declarationModel.Modifiers == ModifierFlags.ReadOnly || declarationModel.Modifiers == ModifierFlags.WriteOnly;
        public static bool IsDataOutput(this IVariableDeclarationModel declarationModel) => declarationModel.Modifiers == ModifierFlags.WriteOnly && declarationModel.DataType != TypeHandle.ExecutionFlow;
        public static bool IsDataInput(this IVariableDeclarationModel declarationModel) => declarationModel.Modifiers == ModifierFlags.ReadOnly && declarationModel.DataType != TypeHandle.ExecutionFlow;

        public static bool IsInputOrOutputTrigger(this IVariableDeclarationModel declarationModel)
        {
            return IsInputOrOutput(declarationModel) && declarationModel.DataType == TypeHandle.ExecutionFlow;
        }

        public static bool IsGraphVariable(this IVariableDeclarationModel declarationModel)
        {
            return ((VariableDeclarationModel)declarationModel).variableFlags == VariableFlags.None &&
                declarationModel.Modifiers == ModifierFlags.None;
        }

        public static bool IsDataNode(this INodeModel nodeModel)
        {
            switch (nodeModel)
            {
                case IConstantNodeModel _:
                    return true;
                case IDotsNodeModel dotsNodeModel:
                    return typeof(IDataNode).IsAssignableFrom(dotsNodeModel.NodeType);
                case VariableNodeModel _:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException($"Don't know if a node of type {nodeModel.GetType().Name} is a Data Node or not");
            }
        }
    }
}
