using System.Collections.Generic;
using Unity.Entities;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine.VisualScripting;

namespace DotsStencil
{
    /// <summary>
    /// Record of a given Entity in the context of a Graph, see <see cref="IGraphTrace"/>
    /// </summary>
    public class GraphTrace : IGraphTrace
    {
        public Entity Entity;
        public readonly string EntityName;
        public CircularBuffer<EntityFrameData> Frames;

        public GraphTrace(Entity entity, string entityName)
        {
            Entity = entity;
            EntityName = entityName;
            Frames = new CircularBuffer<EntityFrameData>(100);
        }

        public IReadOnlyList<IFrameData> AllFrames => Frames;
    }
}
