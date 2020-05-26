using System;
using System.Collections.Generic;
using System.Linq;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace DotsStencil
{
    [Serializable, EnumNodeSearcher(typeof(MathBinaryBool.BinaryBoolType), "Flow")]
    class MathBinaryBoolNodeModel : DotsNodeModel<MathBinaryBool>, IHasMainInputPort, IHasMainOutputPort
    {
        public override string Title => TypedNode.Type.ToString().Nicify();
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }

        [HackContextualMenuVariableCount("Input", min: 2)]
        public int numCases = 2; // TODO allow changing this through the UI

        public override IReadOnlyDictionary<string, List<PortMetaData>> PortCustomData => new Dictionary<string, List<PortMetaData>>
        {
            {nameof(MathBinaryBool.Inputs), Enumerable.Repeat(GetPortMetadata(nameof(MathBinaryBool.Inputs)), numCases).ToList()},
        };
    }
}
