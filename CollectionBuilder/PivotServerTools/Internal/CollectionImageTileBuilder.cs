// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.IO;
    using System.Web;


    /// <summary>
    /// Build a Deep Zoom Collection image tile by drawing item images onto it.
    /// </summary>
    internal class CollectionImageTileBuilder
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public CollectionImageTileBuilder(Collection collection, ImageRequest imageRequest,
            int maxLevel, int tilePixelDimension)
        {
            m_collection = collection;
            m_level = imageRequest.Level;

            m_tilePixelDimension = tilePixelDimension;
            m_imageDimensionCount = (1 << (maxLevel - imageRequest.Level));
            m_levelBitCount = maxLevel - imageRequest.Level;

            int mortonRange;
            m_mortonStart = MortonHelpers.LevelXYToMorton(imageRequest.Level, imageRequest.X, imageRequest.Y,
                maxLevel, out mortonRange);
        }


        // Public Methods
        //======================================================================

        public void Write(HttpResponse response)
        {
            response.ContentType = "image/jpeg";
            Write(response.OutputStream);
        }

        public void Write(Stream outStream)
        {
            SystemWindowsMediaTileImage tile = new SystemWindowsMediaTileImage();
            try
            {
                tile.Create(m_tilePixelDimension, m_level);

                //Draw the sub-images into the tile
                int subImageDimension = m_tilePixelDimension / m_imageDimensionCount;
                for (int y = 0; y < m_imageDimensionCount; ++y)
                {
                    for (int x = 0; x < m_imageDimensionCount; ++x)
                    {
                        int itemIndex = m_mortonStart + MortonHelpers.XYToMorton(m_levelBitCount, x, y);
                        if (itemIndex < m_collection.Items.Count)
                        {
                            CollectionItem item = m_collection.Items[itemIndex];
                            tile.DrawSubImage(item.ImageProvider, x*subImageDimension, y*subImageDimension,
                                subImageDimension, subImageDimension);
                        }
                    }
                }

                tile.WriteTo(outStream);
            }
            finally
            {
                tile.Close();
            }
        }


        // Private Fields
        //======================================================================

        Collection m_collection;
        int m_level;
        int m_tilePixelDimension;
        int m_imageDimensionCount;
        int m_mortonStart;
        int m_levelBitCount;
    }

}
