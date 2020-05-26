using System;
using System.Collections.Generic;
using System.Linq;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;

namespace DotsStencil
{
    [Serializable, DotsSearcherItem("Log")]
    class LogNodeModel : DotsNodeModel<Log>, IHasMainExecutionInputPort, IHasMainExecutionOutputPort, IHasMainInputPort
    {
        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }

        [HackContextualMenuVariableCount("Message")]
        public int numCases = 1; // TODO allow changing this through the UI

        public override IReadOnlyDictionary<string, List<PortMetaData>> PortCustomData => new Dictionary<string, List<PortMetaData>>
        {
            { nameof(Log.Messages), InputPortsMetadata().ToList() }
        };

        IEnumerable<PortMetaData> InputPortsMetadata()
        {
            var defaultData = GetPortMetadata(nameof(Log.Messages));
            for (int i = 0; i < numCases; i++)
            {
                defaultData.Name = $"Message {i + 1}";
                yield return defaultData;
            }
        }
    }
}
