using Moq;
using NUnit.Framework;
using Runtime;
using UnityEditor.VisualScriptingECSTests;

namespace Nodes
{
    public class MathBinaryBoolTests : BaseDataNodeRuntimeTests<MathBinaryBool>
    {
        [TestCase(false, MathBinaryBool.BinaryBoolType.LogicalAnd, false, false)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.LogicalAnd, true, false)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.LogicalAnd, false, false)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.LogicalAnd, true, true)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.LogicalAnd, true, true, true)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.LogicalAnd, true, true, false)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.LogicalOr, false, false)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.LogicalOr, true, true)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.LogicalOr, false, true)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.LogicalOr, true, false)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.LogicalOr, false, false, true)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.LogicalOr, false, false, false)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.Xor, false, false)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.Xor, true, true)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.Xor, false, true)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.Xor, true, false)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.Xor, true, false, false)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.Xor, true, false, true)]
        [TestCase(false, MathBinaryBool.BinaryBoolType.Xor, true, false, true, false, false)]
        [TestCase(true, MathBinaryBool.BinaryBoolType.Xor, true, false, true, true, false)]
        public void TestMathBinaryBool(bool Result, MathBinaryBool.BinaryBoolType type, params bool[] inputs)
        {
            m_Node.Type = type;
            SetupInputs(inputs);
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, Result), Times.Once());
        }

        void SetupInputs(params bool[] inputs)
        {
            m_Node.Inputs.DataCount = inputs.Length;
            CustomSetup();
            for (uint i = 0; i < inputs.Length; i++)
            {
                var port = m_Node.Inputs.SelectPort(i);
                m_GraphInstanceMock.Setup(x => x.ReadBool(port)).Returns(inputs[i]);
            }
        }
    }
}
