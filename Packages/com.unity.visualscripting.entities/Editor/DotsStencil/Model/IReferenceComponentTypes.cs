using System.Collections.Generic;
using Runtime;

namespace DotsStencil
{
    public interface IReferenceComponentTypes : IDotsNodeModel
    {
        IEnumerable<TypeReference> ReferencedTypes { get; }
    }
}
