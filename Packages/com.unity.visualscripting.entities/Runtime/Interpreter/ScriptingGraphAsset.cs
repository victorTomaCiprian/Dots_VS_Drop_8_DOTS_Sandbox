using UnityEngine;

namespace Runtime
{
    public class ScriptingGraphAsset : ScriptableObject
    {
        [SerializeReference]
        public GraphDefinition Definition;
        [SerializeField]
        public uint HashCode;
    }
}
