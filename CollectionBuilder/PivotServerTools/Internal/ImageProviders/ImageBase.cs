// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;


    /// <summary>
    /// The base class for item image implementations that create the image bytes from some source.
    /// Just override the MakeImage() method.
    /// </summary>
    internal abstract class ImageBase : ImageProviderBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public ImageBase()
        {
        }


        // Interface realizations and base class overrides
        //======================================================================

        #region ImageProviderBase Members

        public override System.Drawing.Size Size 
        {
            get 
            {
                EnsureIsSize();
                return m_size;
            }

            protected set
            {
                m_size = value;
            }
        }

        public override byte[] ImageData
        {
            get
            {
                EnsureIsLoaded();
                return m_imageData;
            }
        }

        protected byte[] ImageDataInternal
        {
            get { return m_imageData; }
        }

        public override void Draw(Graphics g, Rectangle itemRectangle, int level)
        {
            EnsureIsLoaded();

            using (MemoryStream stream = new MemoryStream(m_imageData))
            using (Image image = Image.FromStream(stream))
            {
                System.Drawing.Size scaledSize = FileImage.ScaleToFillSize(image.Size, itemRectangle.Size);
                g.DrawImage(image, itemRectangle.X, itemRectangle.Y, scaledSize.Width, scaledSize.Height);
            }
        }

        #endregion


        // Protected Methods
        //======================================================================

        /// <summary>
        /// If the image is a constant size, you may override this to set the size directly.
        /// </summary>
        protected virtual void EnsureIsSize()
        {
            EnsureIsLoaded();
        }

        protected virtual void EnsureIsLoaded()
        {
            //Note, this method can be called concurrently by multiple image-request threads.
            // TODO: For non-sample use, lock the portion of code that loads the image to avoid
            // duplicating work, or implement a shared queue that manages asynchronous loading of all source images.

            if (null == m_imageData)
            {
                try
                {
                    using (Image image = MakeImage())
                    {
                        m_size = image.Size;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            image.Save(stream, ImageFormat.Jpeg);
                            m_imageData = stream.ToArray();
                        }
                    }
                }
                catch { }
            }
        }


        /// <summary>
        /// Override this method to return the desired Image object to display.
        /// </summary>
        protected abstract Image MakeImage();


        // Private Methods
        //======================================================================

        internal static System.Drawing.Size ScaleToFillSize(System.Drawing.Size size, System.Drawing.Size maxSize)
        {
            System.Drawing.Size newSize = new System.Drawing.Size();
            double aspectRatio = ((double)size.Width) / size.Height;
            if (aspectRatio > 1.0)
            {
                newSize.Width = maxSize.Width;
                newSize.Height = (int)((double)newSize.Width / aspectRatio);
            }
            else
            {
                newSize.Height = maxSize.Height;
                newSize.Width = (int)(newSize.Height * aspectRatio);
            }
            return newSize;
        }


        // Private Fields
        //======================================================================
        
        System.Drawing.Size m_size;
        byte[] m_imageData;
    }

}
