using System;
using System.Collections.Generic;
using DotsStencil;
using Runtime;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using ValueType = Runtime.ValueType;

namespace NodeModels
{
    [DotsSearcherItem(k_Title), Serializable]
    class InstantiateAtNodeModel : DotsNodeModel<InstantiateAt>, IHasMainExecutionInputPort,
        IHasMainExecutionOutputPort, IHasMainInputPort, IHasMainOutputPort
    {
        const string k_Title = "Instantiate At";

        public override string Title => k_Title;

        public IPortModel ExecutionInputPort { get; set; }
        public IPortModel ExecutionOutputPort { get; set; }
        public IPortModel InputPort { get; set; }
        public IPortModel OutputPort { get; set; }

        // TODO We should really move ports and nodes description to NodeModel to avoid this
        public override IReadOnlyDictionary<string, List<PortMetaData>> PortCustomData =>
            new Dictionary<string, List<PortMetaData>>
        {
            { nameof(InstantiateAt.Scale), new List<PortMetaData>
              {
                  new PortMetaData
                  {
                      Name = "Scale",
                      Type = ValueType.Float3,
                      DefaultValue = Vector3.one
                  }
              }},
        };
    }
}
