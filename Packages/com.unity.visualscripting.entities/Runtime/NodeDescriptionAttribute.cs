using System;
using JetBrains.Annotations;

namespace Runtime
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public class NodeDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public object Type { get; }

        public NodeDescriptionAttribute(string description)
        {
            Description = description;
        }

        public NodeDescriptionAttribute(object type, string description)
        {
            Type = type;
            Description = description;
        }
    }
}
