using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace DotsStencil
{
    /// <summary>
    /// See <see cref="IFrameData"/>
    /// </summary>
    public class EntityFrameData : IFrameData, IDisposable
    {
        internal class FrameDataComparer : IComparer<EntityFrameData>
        {
            public int Compare(EntityFrameData x, EntityFrameData y)
            {
                if (x == null || y == null)
                    return x == y ? 0 : x == null ? -1 : 1;
                return Comparer<int>.Default.Compare(x.Frame, y.Frame);
            }
        }
        public int Frame { get; }

        public IEnumerable<TracingStep> GetDebuggingSteps(IGraphModel context)
        {
            DotsStencil dotsStencil = (DotsStencil)context.Stencil;

            if (!FrameTrace.IsValid)
                yield break;
            var reader = FrameTrace.AsReader();
            reader.BeginForEachIndex(0);
            var debugger = (DotsDebugger)dotsStencil.Debugger;
            while (reader.RemainingItemCount != 0)
                if (debugger.ReadDebuggingDataModel(ref reader, this, out TracingStep step))
                    yield return step;

            reader.EndForEachIndex();
        }

        public DotsFrameTrace FrameTrace { get; }

        public EntityFrameData(int frame, DotsFrameTrace frameTrace)
        {
            Frame = frame;
            FrameTrace = frameTrace;
        }

        public void Dispose()
        {
            FrameTrace?.Dispose();
        }
    }
}
