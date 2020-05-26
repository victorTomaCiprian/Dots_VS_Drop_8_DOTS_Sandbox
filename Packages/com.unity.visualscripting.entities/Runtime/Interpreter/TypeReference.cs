using System;
using Unity.Entities;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public struct TypeReference
    {
        public ulong TypeHash;

        public int TypeIndex => TypeManager.GetTypeIndexFromStableTypeHash(TypeHash);
        public ComponentType GetComponentType() => ComponentType.FromTypeIndex(TypeIndex);
    }
}
