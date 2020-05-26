using Moq;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathMultiInputTests : BaseDataNodeRuntimeTests<MathGenericNode>
    {
        [TestCase(MathGeneratedFunction.AddIntInt, 47)]
        [TestCase(MathGeneratedFunction.MultiplyIntInt, 210)]
        [TestCase(MathGeneratedFunction.MinIntInt, 5)]
        [TestCase(MathGeneratedFunction.MaxIntInt, 42)]
        public void TestMultiInputMath2Inputs(MathGeneratedFunction function, int result)
        {
            m_Node = m_Node.WithFunction(function);

            SetupInputs(42, 5);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
        }

        [TestCase(MathGeneratedFunction.AddIntInt, 21)]
        [TestCase(MathGeneratedFunction.MultiplyIntInt, 720)]
        [TestCase(MathGeneratedFunction.MinIntInt, 1)]
        [TestCase(MathGeneratedFunction.MaxIntInt, 6)]
        public void TestMultiInputMath(MathGeneratedFunction function, int result)
        {
            m_Node = m_Node.WithFunction(function);

            SetupInputs(1, 2, 3, 4, 5, 6);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, result), Times.Once());
        }

        void SetupInputs(params int[] inputs)
        {
            m_Node.Inputs.DataCount = inputs.Length;
            CustomSetup();
            for (uint i = 0; i < inputs.Length; i++)
            {
                var port = m_Node.Inputs.SelectPort(i);
                m_GraphInstanceMock.Setup(x => x.ReadValue(port)).Returns(inputs[i]);
            }
        }
    }
}
