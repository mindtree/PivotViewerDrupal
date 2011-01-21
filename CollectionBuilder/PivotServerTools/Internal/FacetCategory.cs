// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;


    internal class FacetCategory
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public FacetCategory(string name, FacetType type)
        {
            this.Name = name;
            this.FacetType = type;

            this.IsShowInFacetPane = true;
            this.IsShowInInfoPane = true;
            this.IsTextFilter = true;
        }

        // Public Properties
        //======================================================================

        public string Name { get; private set; }
        public FacetType FacetType { get; set; }

        public string DisplayFormat { get; set; }
        public bool IsShowInFacetPane { get; set; }
        public bool IsShowInInfoPane { get; set; }
        public bool IsTextFilter { get; set; }
    }

}
