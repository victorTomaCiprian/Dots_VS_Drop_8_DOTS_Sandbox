using System.Collections.Generic;
using Unity.Entities;

namespace Runtime
{
    public struct DotsEventData
    {
        public ulong Id { get; }
        public Entity TargetEntity { get; }
        public IEnumerable<Value> Values { get; }

        public DotsEventData(ulong id, IEnumerable<Value> values, Entity targetEntity = default)
        {
            Id = id;
            Values = values;
            TargetEntity = targetEntity;
        }
    }
}
