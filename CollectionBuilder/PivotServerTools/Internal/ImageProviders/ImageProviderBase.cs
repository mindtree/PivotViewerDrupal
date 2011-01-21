// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;


    /// <summary>
    /// Override this class to add custom drawn images.
    /// Note, implementations of this class can be called concurrently by multiple image request threads.
    /// </summary>
    internal abstract class ImageProviderBase
    {
        /// <summary>
        /// Return the size of the image
        /// </summary>
        public virtual System.Drawing.Size Size { get; protected set; }

        /// <summary>
        /// Return the raw byte data of the image.
        /// </summary>
        public abstract byte[] ImageData { get; }

        /// <summary>
        /// The string to put in the DZC XML as the original image source.
        /// Leave this null if not using Deep Zoom Images.
        /// </summary>
        public string DziPath { get; set; }

        /// <summary>
        /// Draw the entire image into the given rectangle in the graphics context.
        /// Use the level parameter to draw different visuals for different levels, if desired.
        /// </summary>
        public abstract void Draw(Graphics g, Rectangle targetRectangle, int level);

        public virtual IDisposable Draw(DrawingContext drawingContext, Rect targetItemRect, int level)
        {
            //Default is to draw the image bits into the context:

            //Do not dispose of the memory stream here, because System.Media.Windows uses
            // retained mode rendering where the commands get batched to execute later.
            MemoryStream imageStream = new MemoryStream(this.ImageData);
            try
            {
                TransformedBitmap shrunkImage = ResizeJpeg(imageStream, targetItemRect.Size);

                //DrawingContext.DrawImage will scale an image to fill the size, so modify
                // our target rect to be exactly the correct image position on the tile.
                Rect targetImageRect = new Rect(targetItemRect.X, targetItemRect.Y,
                    shrunkImage.PixelWidth, shrunkImage.PixelHeight);
                drawingContext.DrawImage(shrunkImage, targetImageRect);

                return imageStream; //Return our stream so it can be disposed later.
            }
            catch
            {
                if (null != imageStream)
                {
                    imageStream.Dispose();
                }
                throw;
            }
        }

        public virtual IDisposable DrawPortion(DrawingContext drawingContext, Rect targetItemRectangle,
            Rect sourceRectangle, int level)
        {
            //Do not dispose the memory stream here, because System.Media.Windows uses
            // retained mode rendering where the commands get batched to execute later.
            MemoryStream imageStream = new MemoryStream(this.ImageData);
            try
            {
                BitmapSource source = BitmapFromJpeg(imageStream);
                Int32Rect cropRect = Int32RectFromRect(sourceRectangle);
                CroppedBitmap croppedImage = new CroppedBitmap(source, cropRect);
                drawingContext.DrawImage(croppedImage, targetItemRectangle);
                return imageStream; //Return our stream so it can be disposed later.
            }
            catch
            {
                if (null != imageStream)
                {
                    imageStream.Dispose();
                }
                throw;
            }
        }

        private static Int32Rect Int32RectFromRect(Rect inputRect)
        {
            Int32RectConverter converter = new Int32RectConverter();
            if (converter.CanConvertFrom(inputRect.GetType()))
            {
                return (Int32Rect)converter.ConvertFrom(inputRect);
            }
            else
            {
                Int32Rect outRect = new Int32Rect((int)inputRect.X, (int)inputRect.Y,
                    (int)inputRect.Width, (int)inputRect.Height);
                return outRect;
            }
        }


        /// <summary>
        /// Resize a JPEG image contained in a stream to fit the given size, preserving the image aspect ratio.
        /// </summary>
        private static TransformedBitmap ResizeJpeg(Stream jpegImageStream, System.Windows.Size maxSize)
        {
            BitmapSource source = BitmapFromJpeg(jpegImageStream);

            //Reduce the image by the correct power of 2 so it fits.
            int largestDimension = Math.Max(source.PixelWidth, source.PixelHeight);
            int reductionFactor = 1;
            while (largestDimension > maxSize.Width)
            {
                largestDimension >>= 1;
                reductionFactor <<= 1;
            }
            double scaleFactor = 1.0 / reductionFactor;
            ScaleTransform scalingTransform = new ScaleTransform(scaleFactor, scaleFactor);

            return new TransformedBitmap(source, scalingTransform);
        }

        private static BitmapSource BitmapFromJpeg(Stream jpegImageStream)
        {
            JpegBitmapDecoder decoder = new JpegBitmapDecoder(jpegImageStream,
                BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
            BitmapSource source = decoder.Frames[0];
            return source;
        }
    }

}
