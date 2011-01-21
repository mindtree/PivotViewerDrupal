// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Web;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;


    /// <summary>
    /// Create an image tile within a Deep Zoom Image pyramid.
    /// </summary>
    internal class DeepZoomImageTile
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public DeepZoomImageTile(ImageProviderBase image, DeepZoomImageRequest imageRequest,
            int tilePixelDimension, int overlap, string format)
        {
            m_image = image;
            m_imageRequest = imageRequest;
            m_level = imageRequest.Level;
            m_tilePixelDimension = tilePixelDimension;
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
            BitmapSource bitmap = MakeTileBitmap();
            if (null != bitmap)
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    WriteJpgToStream(memStream, bitmap);
                    memStream.Position = 0; //Rewind the stream

                    //Note, .NET 4.0 has added a CopyStream method that can be used here.
                    CopyStream(memStream, outStream);
                }
            }
        }


        // Private Methods
        //======================================================================

        private BitmapSource MakeTileBitmap()
        {
            const double dotsPerInch = 96.0;

            IDisposable disposable = null;
            try
            {
                Rect sourceRect, targetRect;
                if (!MakeSourceAndTargetRects(out sourceRect, out targetRect))
                {
                    return null;
                }

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    disposable = m_image.DrawPortion(drawingContext, targetRect, sourceRect, m_level);
                }

                RenderTargetBitmap tileBitmap = new RenderTargetBitmap((int)targetRect.Width, (int)targetRect.Height,
                    dotsPerInch, dotsPerInch, PixelFormats.Default);
                tileBitmap.Render(drawingVisual);
                return tileBitmap;
            }
            finally
            {
                if(null != disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private bool MakeSourceAndTargetRects(out Rect sourceRect, out Rect targetRect)
        {
            System.Drawing.Size worldSize = m_image.Size;
            int worldLevel = GetWorldLevel(worldSize);
            int levelDelta = worldLevel - m_imageRequest.Level;
            if (levelDelta < 0)
            {
                sourceRect = targetRect = Rect.Empty;
                return false; //Trying to request more detail than exists.
            }

            Rectangle worldRect = new Rectangle(new System.Drawing.Point(0, 0), m_image.Size);

            int tileDimension = DziSerializer.DefaultTileSize; //This is only true for overlap=0.
            int viewDimension = tileDimension << levelDelta;
            Rectangle viewRect = new Rectangle(m_imageRequest.X * viewDimension, m_imageRequest.Y * viewDimension,
                viewDimension, viewDimension);
            viewRect.Intersect(worldRect); //clamp the view so it isn't larger than the world rect.

            //Now calculate the target size, which is the view rect scaled down.
            int targetWidth = viewRect.Width >> levelDelta;
            int targetHeight = viewRect.Height >> levelDelta;

            sourceRect = new Rect(viewRect.X, viewRect.Y, viewRect.Width, viewRect.Height);
            targetRect = new Rect(0, 0, targetWidth, targetHeight);
            return true;
        }

        int GetWorldLevel(System.Drawing.Size worldSize)
        {
            int worldMaxDimension = Math.Max(worldSize.Width, worldSize.Height);
            return MaxLevelForDimension(worldMaxDimension);
        }

        private int MaxLevelForDimension(int dimension)
        {
            --dimension;
            int maxLevel = 0;
            while (dimension > 0)
            {
                dimension >>= 1;
                ++maxLevel;
            }
            return maxLevel;
        }

        private void WriteJpgToStream(Stream stream, BitmapSource bitmap)
        {
            BitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream); //Note, cannot save to an HttpResponseStream
        }

        private static void CopyStream(Stream input, Stream output)
        {
            const int maxBufferLength = 65536; //Limit the size of buffer used to copy a stream.

            int bufferLength = Math.Min((int)input.Length, maxBufferLength);
            byte[] buffer = new byte[bufferLength];

            int countBytesRead;
            while ((countBytesRead = input.Read(buffer, 0, bufferLength)) > 0)
            {
                output.Write(buffer, 0, countBytesRead);
            }
        }


        // Private Fields
        //======================================================================

        ImageProviderBase m_image;
        DeepZoomImageRequest m_imageRequest;
        int m_level;
        int m_tilePixelDimension;
    }

}
