// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Net;


    /// <summary>
    /// Create an item image by loading it from a URL.
    /// </summary>
    internal class WebImage : ImageBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public WebImage(Uri url)
        {
            m_url = url;
        }


        // Protected Methods
        //======================================================================

        protected override Image MakeImage()
        {
            using (WebClient web = new WebClient())
            {
                web.UseDefaultCredentials = true;

                using (Stream webStream = web.OpenRead(m_url))
                {
                    return Image.FromStream(webStream);
                }
            }
        }


        // Private Fields
        //======================================================================

        Uri m_url;
    }

}
