// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// Used internally to represent an item within a collection
    /// </summary>
    internal class CollectionItem
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public CollectionItem()
        {
        }

        // Public Properties
        //======================================================================

        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public ImageProviderBase ImageProvider { get; private set;  }
        public ICollection<Facet> FacetValues { get; set; }


        // Public Methods
        //======================================================================

        public void SetImage(ItemImage image)
        {
            if (null == image)
            {
                //No image set, so draw one ourselves
                this.ImageProvider = new DynamicImage(this.Name, this.Description);
            }
            else if (!string.IsNullOrEmpty(image.ImageFilePath))
            {
                if (image.ImageFilePath.EndsWith(".dzi", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.ImageProvider = new DeepZoomImage(image.ImageFilePath);
                }
                else
                {
                    this.ImageProvider = new FileImage(image.ImageFilePath);
                }
            }
            else if (null != image.ImageUrl)
            {
                if (image.ImageUrl.LocalPath.EndsWith(".dzi", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.ImageProvider = new DeepZoomImage(image.ImageUrl);
                }
                else
                {
                    this.ImageProvider = new WebImage(image.ImageUrl);
                }
            }
        }
    }

}
