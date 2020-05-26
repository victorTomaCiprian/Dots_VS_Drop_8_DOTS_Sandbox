using System;
using Unity.GraphElements;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IBadgeContainer
    {
        IconBadge ErrorBadge { get; set; }
        ValueBadge ValueBadge { get; set; }
    }
}
