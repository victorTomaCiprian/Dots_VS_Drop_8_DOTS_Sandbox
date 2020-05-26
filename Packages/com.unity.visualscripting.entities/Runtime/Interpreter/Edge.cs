using System;

namespace Runtime
{
    [Serializable]
    public struct Edge
    {
        public Port Output;
        public Port Input;

        public Edge(OutputTriggerPort output, InputTriggerPort input)
        {
            Output = output.Port;
            Input = input.Port;
        }

        public Edge(IOutputDataPort output, IInputDataPort input)
        {
            Output = output.GetPort();
            Input = input.GetPort();
        }
    }
}
