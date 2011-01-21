// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.IO;
    using System.Web;


    /// <summary>
    /// Override this class if you choose to implement your own collection tile drawing routines.
    /// </summary>
    internal abstract class TiledImageBase
    {
        public abstract void Create(int tileDimension, int level);

        public virtual void Close() { }

        public abstract void DrawSubImage(ImageProviderBase image, int x, int y, int width, int height);

        public abstract void WriteTo(Stream stream);

        public virtual void WriteTo(HttpResponse response)
        {
            response.ContentType = "image/jpeg";
            WriteTo(response.OutputStream);
        }
    }

}
