// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;


    /// <summary>
    /// Serialize a Collection's image data into Deep Zoom Collection XML.
    /// Reference: http://www.getpivot.com/developer-info/image-content.aspx
    /// </summary>
    internal class DziSerializer
    {
        public static int DefaultTileSize = 256; //Must be a power of 2.
        public static int DefaultOverlap = 0; //DeepZoomImageTile currently assumes this is always 0.
        public static string DefaultFormat = "jpg";


        /// <summary>
        /// Write a collection's image data as a DZC into a TextWriter.
        /// </summary>
        public static void Serialize(TextWriter textWriter, Size size)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(textWriter))
            {
                Serialize(xmlWriter, size);
            }
        }

        /// <summary>
        /// Write a collection's image data as a DZC into an XmlWriter.
        /// </summary>
        static void Serialize(XmlWriter xmlWriter, Size size)
        {
            DziSerializer dzi = new DziSerializer(size);
            dzi.Write(xmlWriter);
        }


        // Constructors, Finalizer and Dispose
        //======================================================================

        private DziSerializer(Size size)
        {
            m_size = size;
        }


        // Public Methods
        //======================================================================

        public static string MakeDziPath(string collectionKey, int itemId)
        {
            return string.Format("{0}/dzi/{1}.dzi", collectionKey, itemId);
        }


        // Private Properties
        //======================================================================

        XNamespace Xmlns
        {
            get { return "http://schemas.microsoft.com/deepzoom/2008"; }
        }


        // Private Methods
        //======================================================================

        private void Write(XmlWriter outputWriter)
        {
            outputWriter.WriteStartDocument();

            XElement root = new XElement(Xmlns + "Image",
                new XAttribute("TileSize", DefaultTileSize),
                new XAttribute("Overlap", DefaultOverlap),
                new XAttribute("Format", DefaultFormat),
                new XElement(Xmlns + "Size",
                        new XAttribute("Width", m_size.Width),
                        new XAttribute("Height", m_size.Height)));
            root.WriteTo(outputWriter);
        }


        // Private Fields
        //======================================================================

        Size m_size;
    }

}
