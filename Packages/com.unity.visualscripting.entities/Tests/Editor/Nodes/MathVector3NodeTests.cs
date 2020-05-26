using Moq;
using NUnit.Framework;
using Runtime;
using Runtime.Mathematics;
using Unity.Mathematics;
using UnityEditor.VisualScriptingECSTests;
using UnityEngine.PlayerLoop;

namespace Nodes
{
    public class MathVector3NodeTests : BaseDataNodeRuntimeTests<MathGenericNode>
    {
        [TestCase(MathGeneratedFunction.AddFloat3Float3, 1f, 3f, 5f)]
        [TestCase(MathGeneratedFunction.SubtractFloat3Float3, 1f, 1f, 1f)]
        [TestCase(MathGeneratedFunction.CrossFloat3Float3, 1f, -2f, 1f)]
        public void TestRuntimeBinaryNumber(MathGeneratedFunction function, float xResult, float yResult, float zResult)
        {
            m_Node = m_Node.WithFunction(function);
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Inputs.SelectPort(0))).Returns(new float3(1, 2, 3));
            m_GraphInstanceMock.Setup(x => x.ReadValue(m_Node.Inputs.SelectPort(1))).Returns(new float3(0, 1, 2));
            ExecuteNode();
            m_GraphInstanceMock.Verify(x => x.Write(m_Node.Result, new float3(xResult, yResult, zResult)), Times.Once());
        }
    }
}
