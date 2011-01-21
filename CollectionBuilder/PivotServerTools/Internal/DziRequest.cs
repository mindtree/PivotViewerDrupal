// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Text.RegularExpressions;


    /// <summary>
    /// Translates the URL query parameters for a DZI XML request into useful properties.
    /// </summary>
    internal class DziRequest
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public DziRequest(Uri url)
        {
            Match match = s_matcher.Match(url.AbsolutePath);

            if (match.Groups.Count != 3)
            {
                throw new ArgumentException();
            }

            this.CollectionKey = match.Groups[1].Value;
            this.ItemId = int.Parse(match.Groups[2].Value);
        }


        // Public Properties
        //======================================================================

        public string CollectionKey { get; private set; }
        public int ItemId { get; private set; }


        // Private Fields
        //======================================================================

        static readonly Regex s_matcher = new Regex(".*/(.*)/dzi/(.*).dzi", RegexOptions.Compiled
            | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

}
