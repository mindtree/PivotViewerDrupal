// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;


    /// <summary>
    /// Draw collection tiles by using the System.Windows.Media imaging objects.
    /// </summary>
    class SystemWindowsMediaTileImage : TiledImageBase
    {
        public override void Create(int tileDimension, int level)
        {
            m_tileDimension = tileDimension;
            m_level = level;

            m_visual = new DrawingVisual();
            m_context = m_visual.RenderOpen();

            m_disposables = new List<IDisposable>();
        }

        public override void Close()
        {
            CloseContext();
            CloseDisposables();
        }

        public override void DrawSubImage(ImageProviderBase image, int x, int y, int width, int height)
        {
            Rect itemRect = new Rect(x, y, width, height);
            IDisposable disposeAfterRender = image.Draw(m_context, itemRect, m_level);
            if (null != disposeAfterRender)
            {
                //add the disposable to our list.
                m_disposables.Add(disposeAfterRender);
            }
        }

        public override void WriteTo(Stream stream)
        {
            //EndDrawSubImages
            CloseContext();

            const double dotsPerInch = 96.0;

            RenderTargetBitmap tileBitmap = new RenderTargetBitmap(m_tileDimension, m_tileDimension,
                dotsPerInch, dotsPerInch, PixelFormats.Default);
            tileBitmap.Render(m_visual);

            CloseDisposables();

            //Write
            using (MemoryStream memStream = new MemoryStream())
            {
                WriteJpgToStream(memStream, tileBitmap);
                memStream.Position = 0; //Rewind the stream

                //Note, .NET 4.0 has added a CopyStream method.
                CopyStream(memStream, stream);
            }
        }


        // Private Methods
        //======================================================================

        void CloseContext()
        {
            if (null != m_context)
            {
                m_context.Close();
                m_context = null;
            }
        }

        void CloseDisposables()
        {
            if (null != m_disposables)
            {
                foreach (IDisposable d in m_disposables)
                {
                    d.Dispose();
                }
                m_disposables = null;
            }
        }

        void WriteJpgToStream(Stream stream, BitmapSource bitmap)
        {
            BitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream); //Note, cannot save to an HttpResponseStream
        }

        static void CopyStream(Stream input, Stream output)
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

        int m_tileDimension;
        int m_level;
        DrawingVisual m_visual;
        DrawingContext m_context;
        List<IDisposable> m_disposables;
    }
}
