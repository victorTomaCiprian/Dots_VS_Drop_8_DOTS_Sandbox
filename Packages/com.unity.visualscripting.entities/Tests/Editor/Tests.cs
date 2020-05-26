using NodeModels;
using NUnit.Framework;
using Runtime.Mathematics;

namespace VisualScripting.Tests
{
    public class Tests
    {
        [Test]
        public void TestCircularBufferIsWorking()
        {
            Assert.Pass();
        }

        [Test]
        public void TestMathCodeGenVersion()
        {
            Assert.That(MathCodeGeneration.GetVersion(), Is.EqualTo(MathGeneratedDelegates.GenerationVersion));
        }
    }
}
