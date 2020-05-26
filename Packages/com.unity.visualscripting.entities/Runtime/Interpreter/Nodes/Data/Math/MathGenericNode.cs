using System;
using Runtime.Mathematics;
using Runtime.Nodes;
using Unity.Assertions;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct MathGenericNode : IDataNode, IHasExecutionType<MathGeneratedFunction>
    {
        [SerializeField]
        int m_GenerationVersion;
        public int GenerationVersion
        {
            get => m_GenerationVersion;
            set => m_GenerationVersion = value;
        }

        public MathGeneratedFunction Function;

        [PortDescription("", ValueType.Float)]
        public InputDataMultiPort Inputs;
        [PortDescription("", ValueType.Float)]
        public OutputDataPort Result;

        public MathGeneratedFunction Type
        {
            get => Function;
            set => Function = value;
        }

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            Assert.AreEqual(GenerationVersion, MathGeneratedDelegates.GenerationVersion);
            var result = ctx.ApplyBinMath(Inputs, Function);
            ctx.Write(Result, result);
        }
    }
}
