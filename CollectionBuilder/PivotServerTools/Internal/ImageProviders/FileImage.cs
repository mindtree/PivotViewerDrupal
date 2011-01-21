// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;


    /// <summary>
    /// Create an item image by loading it from a file.
    /// </summary>
    internal class FileImage : ImageBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public FileImage(string filePath)
        {
            m_filePath = filePath;
        }


        // Protected Methods
        //======================================================================

        protected override Image MakeImage()
        {
            return Image.FromFile(m_filePath);
        }


        // Private Fields
        //======================================================================

        string m_filePath;
    }

}
