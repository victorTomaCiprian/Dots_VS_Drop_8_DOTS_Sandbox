using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using ValueType = Runtime.ValueType;

namespace DotsStencil
{
    class DotsSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        Stencil m_Stencil;
        List<SearcherDatabase> m_GraphElementsSearcherDatabases;

        public DotsSearcherDatabaseProvider(Stencil mStencil)
        {
            m_Stencil = mStencil;
        }

        public List<SearcherDatabase> GetGraphElementsSearcherDatabases()
        {
            return m_GraphElementsSearcherDatabases ?? (m_GraphElementsSearcherDatabases = new List<SearcherDatabase>
            {
                new GraphElementSearcherDatabase(m_Stencil)
                    .AddNodesWithSearcherItemAttribute()
                    .AddNodesWithSearcherItemCollectionAttribute()
                    .AddDotsConstants()
                    .AddDotsEvents()
                    .AddStickyNote()
                    .AddEdgePortals() // TODO temp while developing. Will not be created from searcher in the long run.
                    .Build()
            });
        }

        public List<SearcherDatabase> GetReferenceItemsSearcherDatabases()
        {
            return new List<SearcherDatabase>();
        }

        public List<SearcherDatabase> GetTypesSearcherDatabases()
        {
            return new List<SearcherDatabase>
            {
                new TypeSearcherDatabase(m_Stencil, m_Stencil.GetAssembliesTypesMetadata())
                    .AddBasicDotsTypes()
                    .AddTypesInheritingFrom<IComponentData>()
                    .Build()
            };
        }

        public List<SearcherDatabase> GetTypeMembersSearcherDatabases(TypeHandle typeHandle)
        {
            return new List<SearcherDatabase>();
        }

        public List<SearcherDatabase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel,
            IFunctionModel functionModel = null)
        {
            return new List<SearcherDatabase>();
        }

        public List<SearcherDatabase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabase>();
        }

        public void ClearGraphElementsSearcherDatabases()
        {
            throw new NotImplementedException();
        }

        public void ClearReferenceItemsSearcherDatabases()
        {
            throw new NotImplementedException();
        }

        public void ClearTypesItemsSearcherDatabases()
        {
            throw new NotImplementedException();
        }

        public void ClearTypeMembersSearcherDatabases()
        {
            throw new NotImplementedException();
        }

        public void ClearGraphVariablesSearcherDatabases()
        {
            throw new NotImplementedException();
        }
    }
}
