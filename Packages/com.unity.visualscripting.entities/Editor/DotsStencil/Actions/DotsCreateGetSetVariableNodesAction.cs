using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace DotsStencil
{
    public class DotsCreateGetSetVariableNodesAction : IAction
    {
        public readonly List<Tuple<IVariableDeclarationModel, Vector2>> VariablesToCreate;
        public readonly bool CreateGetters;

        public DotsCreateGetSetVariableNodesAction(List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate,
                                                   bool createGetters)
        {
            VariablesToCreate = variablesToCreate;
            CreateGetters = createGetters;
        }
    }
}
