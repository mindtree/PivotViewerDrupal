// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Text.RegularExpressions;


    /// <summary>
    /// Translates the URL query parameters for a tile image request into useful properties.
    /// </summary>
    internal class ImageRequest
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public ImageRequest(Uri url)
        {
            Match match = s_matcher.Match(url.AbsolutePath);

            if (match.Groups.Count != 5)
            {
                throw new ArgumentException();
            }

            this.DzcName = match.Groups[1].Value;
            this.Level = int.Parse(match.Groups[2].Value);
            this.X = int.Parse(match.Groups[3].Value);
            this.Y = int.Parse(match.Groups[4].Value);

        }


        // Public Properties
        //======================================================================

        public string DzcName { get; private set; }
        public int Level { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }


        // Private Fields
        //======================================================================

        static readonly Regex s_matcher = new Regex(".*/(.*)_files/(.*)/(.*)_(.*).jpg", RegexOptions.Compiled
            | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

}
