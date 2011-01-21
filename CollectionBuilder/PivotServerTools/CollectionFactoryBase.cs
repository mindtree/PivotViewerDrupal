// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools
{
    using System;
    using System.Collections.Specialized;
    using System.Collections.Generic;


    /// <summary>
    /// Contains data about the CXML query, passed to overrides of CollectionFactoryBase.MakeCollection().
    /// </summary>
    public class CollectionRequestContext
    {
        internal CollectionRequestContext(NameValueCollection query, string collectionUrl)
        {
            this.Query = query;
            this.Url = collectionUrl;
        }

        /// <summary>
        /// The parameters passed in the URL
        /// </summary>
        public NameValueCollection Query { get; private set; }

        /// <summary>
        /// The base URL to the collection, without the query parameters.
        /// </summary>
        public string Url { get; private set; }

        //public void OutputTrace(string message);
    }


    /// <summary>
    /// Your collection factory must derive from this class.
    /// Derived instances will be detected and loaded automatically,
    ///   so must have a public constructor that takes no parameters.
    /// </summary>
    public abstract class CollectionFactoryBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        protected CollectionFactoryBase()
        {
        }


        // Public Properties
        //======================================================================

        /// <summary>
        /// The filebody used in the URL to refer to this collection.
        /// If null, the classname is used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A summary of the collection, including URL query parameters that it accepts.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// A list of sample queries that can be appended after the '?' in the URL.
        /// </summary>
        public ICollection<string> SampleQueries { get; set; }


        // Public Methods
        //======================================================================

        /// <summary>
        /// Override this method to provide a Collection object for the request.
        /// </summary>
        public abstract Collection MakeCollection(CollectionRequestContext context);
    }

}
