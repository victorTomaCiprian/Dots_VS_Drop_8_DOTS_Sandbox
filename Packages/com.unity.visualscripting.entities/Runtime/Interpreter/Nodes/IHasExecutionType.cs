using System;
using JetBrains.Annotations;

namespace Runtime.Nodes
{
    public interface IHasExecutionType<T>
    {
        [UsedImplicitly]
        T Type { get; set; }
    }
}
