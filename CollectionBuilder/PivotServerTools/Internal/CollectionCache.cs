// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Web;
    using System.Web.Caching;


    /// <summary>
    /// A cache of collection objects, to use for servicing the subsequent requests for DZC and collection image tiles.
    /// This implementation just uses the global HttpRuntime Cache object. For a multi-server farm,
    /// use a centralized or distributed cache so that all the servers have access to the same cached objects.
    /// </summary>
    public class CollectionCache
    {
        // Public static methods
        //======================================================================

        public static CollectionCache Instance
        {
            get
            {
                if (null == s_instance)
                {
                    s_instance = new CollectionCache();
                }
                return s_instance;
            }
        }


        // Constructors, Finalizer and Dispose
        //======================================================================

        public CollectionCache()
        {
        }

        // Public Methods
        //======================================================================

        public void Add(string key, Collection collection)
        {
            HttpRuntime.Cache.Insert(key, collection, null, Cache.NoAbsoluteExpiration, s_cacheExpiryDuration);
        }

        public Collection Get(string key)
        {
            object o = HttpRuntime.Cache.Get(key);
            return (Collection)o;
        }


        // Private Fields
        //======================================================================

        /// <summary>
        /// The default cache expiration policy is set to a 20 minute sliding window.
        /// </summary>
        static readonly TimeSpan s_cacheExpiryDuration = new TimeSpan(0, 20, 0);

        static CollectionCache s_instance;
    }

}
