using System;
using System.Reflection;
using Unity.Entities;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace DotsStencil
{
    public static class SearcherFilterExtensions
    {
        public static SearcherFilter WithExecutionInputNodes(this SearcherFilter self)
        {
            self.RegisterNode(data => typeof(IHasMainExecutionInputPort).IsAssignableFrom(data.Type));
            return self;
        }

        public static SearcherFilter WithExecutionOutputNodes(this SearcherFilter self)
        {
            self.RegisterNode(data => typeof(IHasMainExecutionOutputPort).IsAssignableFrom(data.Type));
            return self;
        }

        public static SearcherFilter WithDataInputNodes(this SearcherFilter self)
        {
            self.RegisterNode(data => typeof(IHasMainInputPort).IsAssignableFrom(data.Type));
            return self;
        }

        public static SearcherFilter WithDataOutputNodes(this SearcherFilter self)
        {
            self.RegisterNode(data => typeof(IHasMainOutputPort).IsAssignableFrom(data.Type));
            return self;
        }

        public static SearcherFilter WithComponentTypes(this SearcherFilter self, Stencil stencil)
        {
            return self.WithTypesInheriting<IComponentData>(stencil);
        }

        public static SearcherFilter WithAuthoringComponentTypes(this SearcherFilter self, Stencil stencil)
        {
            return self.WithTypesInheriting<IComponentData, GenerateAuthoringComponentAttribute>(stencil);
        }
    }
}
