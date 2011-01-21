// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;


    /// <summary>
    /// A dictionary of classes that implement CollectionFactoryBase, for finding
    /// which factory object to use for a particular CXML request
    /// </summary>
    internal class CollectionFactories
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public CollectionFactories()
        {
        }


        // Public Properties
        //======================================================================

        public int Count
        {
            get { return m_factories.Count; }
        }


        // Public Methods
        //======================================================================

        public void Add(CollectionFactoryBase collectionFactory)
        {
            m_factories.Add(collectionFactory);
        }

        public void AddFromFolder(string folderPath)
        {
            IEnumerable<CollectionFactoryBase> factories = FactoryClassFinder.Find(folderPath);
            foreach (var f in factories)
            {
                m_factories.Add(f);
            }
        }

        //Note, if a given factory is not already loaded, this method does not attempt to find an
        // assembly containing new factories.
        public CollectionFactoryBase Get(string name)
        {
            return m_factories.TryGet(name);
        }

        public IEnumerable<CollectionFactoryBase> EnumerateFactories()
        {
            return m_factories;
        }


        // Private Fields
        //======================================================================

        FactoryBaseCollection m_factories = new FactoryBaseCollection();
    }


    /// <summary>
    /// A dictionary of CollectionFactoryBase objects, indexed by their name
    /// </summary>
    class FactoryBaseCollection : SynchronizedKeyedCollection<string, CollectionFactoryBase>
    {
        public CollectionFactoryBase TryGet(string name)
        {
            CollectionFactoryBase output;
            if (null != base.Dictionary)
            {
                return base.Dictionary.TryGetValue(name.ToLowerInvariant(), out output) ? output : null;
            }
            else
            {
                return base.Items.Find(item => (0 == string.Compare(name, item.Name, true)));
            }
        }

        protected override string GetKeyForItem(CollectionFactoryBase item)
        {
            return item.Name.ToLowerInvariant();
        }
    }

}
