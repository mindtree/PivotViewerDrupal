// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Xml.Linq;


    /// <summary>
    /// Create a collection item image from a Deep Zoom Image.
    /// </summary>
    internal class DeepZoomImage : ImageBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        /// <summary>
        /// Create an image based on a Deep Zoom Image.
        /// Using this constructor will download and parse the DZI file, so may cause a performance hit.
        /// Prefer using the DeepZoomImage constructor where you specify the imageExtension, width and height,
        /// e.g. by storing these values with the information used to build the Collection object.
        /// </summary>
        public DeepZoomImage(Uri url)
            : this(url, null, invalidDimension_c, invalidDimension_c)
        {
        }

        /// <summary>
        /// Create an image based on a Deep Zoom Image, using a Deep Zoom Image pyramid
        /// stored in a file and folder hierarchy.
        /// </summary>
        public DeepZoomImage(string filePath)
            : this(new Uri("file://" + filePath))
        {
        }

        /// <summary>
        /// Create an image based on a Deep Zoom Image.
        /// This constructor allows the image extension ("jpg", "png"), width and height to specified
        /// which avoids the server downloading the DZI XML to get those values.
        /// </summary>
        public DeepZoomImage(Uri dziXmlUrl, string imageExtension, int width, int height)
        {
            m_url = dziXmlUrl;
            m_imageExtension = imageExtension;
            m_width = width;
            m_height = height;

            this.DziPath = dziXmlUrl.AbsoluteUri;
        }


        // Protected Methods
        //======================================================================

        protected override void EnsureIsSize()
        {
            if (m_width < 0)
            {
                //Set the image size by loading the values from the DZI file.
                LoadDziXml();
                this.Size = new Size(m_width, m_height);
            }
        }

        private void LoadDziXml()
        {
            string dziXml;
            using (WebClient web = new WebClient())
            {
                web.UseDefaultCredentials = true;

                //TODO: For non-sample use, evaluate whether to use asynchronous methods to release
                // this web server thread back to the IIS threadpool while waiting for download.
                // For details, see http://msdn.microsoft.com/en-us/magazine/cc164128.aspx
                dziXml = web.DownloadString(m_url);
            }

            XElement xmlImage = XElement.Parse(dziXml);
            if(xmlImage.Name.LocalName != "Image")
            {
                throw new InvalidDataException("Root element is not \"Image\": " + m_url);
            }

            XNamespace xmlns = "http://schemas.microsoft.com/deepzoom/2008";

            m_imageExtension = xmlImage.Attribute("Format").Value;

            XElement xmlSize = xmlImage.Element(xmlns + "Size");
            m_width = int.Parse(xmlSize.Attribute("Width").Value);
            m_height = int.Parse(xmlSize.Attribute("Height").Value);
        }

        protected override void EnsureIsLoaded()
        {
            if (null != ImageDataInternal)
            {
                return;
            }

            //Save the image size we loaded from the DZI XML because base.EnsureIsLoaded() will
            // overwrite it with the size of the level 8 image we load from the pyramid.
            Size realSize = this.Size;

            base.EnsureIsLoaded();

            //Set the full size again.
            this.Size = realSize;
        }

        protected override Image MakeImage()
        {
            //Use the level 8 DZI image for drawing onto the collection tile, because it fits into 256x256.
            Uri imageUrl = GetSubImageUrl(m_url, m_imageExtension, 8, 0, 0);

            using (WebClient web = new WebClient())
            {
                web.UseDefaultCredentials = true;
                using (Stream webStream = web.OpenRead(imageUrl))
                {
                    return Image.FromStream(webStream);
                }
            }
        }


        // Private Methods
        //======================================================================

        private static Uri GetSubImageUrl(Uri dziUrl, string imageExtension, int level, int x, int y)
        {
            //Take the Url to the dzi and strip off the extension.
            string fullUrl = dziUrl.AbsoluteUri;
            string baseUrl = fullUrl.Substring(0, fullUrl.LastIndexOf('.'));

            string imageUrl = string.Format("{0}_files/{1}/{2}_{3}.{4}", baseUrl, level, x, y, imageExtension);
            return new Uri(imageUrl);
        }


        // Private Fields
        //======================================================================

        const int invalidDimension_c = -1;

        Uri m_url;
        string m_imageExtension; //The file extension of images within the DZI pyramid, without the period, e.g. "jpg", "jpeg", "png".
        int m_width;
        int m_height;
    }

}
