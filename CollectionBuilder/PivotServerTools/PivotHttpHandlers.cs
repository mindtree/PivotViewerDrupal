// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Web;
    using PivotServerTools.Internal;


    public static class PivotHttpHandlers
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        static PivotHttpHandlers()
        {
            s_lock = new ReaderWriterLockSlim();
        }


        // Public Methods
        //======================================================================

        public static void ApplicationStart()
        {
            MakeImplementation();
        }

        public static void ApplicationEnd()
        {
            //TODO: How to synchronize this.
            //ClearImplementation();
            if (null != s_lock)
            {
                s_lock.Dispose();
                s_lock = null;
            }
        }


        public static void AddFactoriesFromFolder(string folderPath)
        {
            GetImplementation().AddFactoriesFromFolder(folderPath);
        }

        /// <summary>
        /// Get the collection factory names, descriptions and sample URLs hosted by this server, as an HTML fragment
        /// </summary>
        /// <returns></returns>
        public static string CollectionInfoHtml()
        {
            return GetImplementation().CollectionInfoHtml();
        }

        /// <summary>
        /// Get the list of sample URLs hosted by this server.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> CollectionSampleUrls()
        {
            return GetImplementation().CollectionSampleUrls();
        }


        /// <summary>
        /// Return a CXML response by using the relevant Collection Factory for this request.
        /// </summary>
        public static void ServeCxml(HttpContext context)
        {
            GetImplementation().ServeCxml(context);
        }

        /// <summary>
        /// Return a CXML response using the given Collection object.
        /// </summary>
        public static void ServeCxml(HttpContext context, Collection collection)
        {
            GetImplementation().ServeCxml(context, collection, null);
        }

        public static void ServeDzc(HttpContext context)
        {
            GetImplementation().ServeDzc(context);
        }

        public static void ServeImageTile(HttpContext context)
        {
            GetImplementation().ServeImageTile(context);
        }

        public static void ServeDzi(HttpContext context)
        {
            GetImplementation().ServeDzi(context);
        }

        public static void ServeDeepZoomImage(HttpContext context)
        {
            GetImplementation().ServeDeepZoomImage(context);
        }


        // Private Methods
        //======================================================================

        static void MakeImplementation()
        {
            s_lock.EnterWriteLock();
            try
            {
                //If we're setting this, existing threads will still have their old implementation to use.
                s_impl = new PivotHttpHandlersImpl();
            }
            finally
            {
                s_lock.ExitWriteLock();
            }
        }

        static PivotHttpHandlersImpl GetImplementation()
        {
            s_lock.EnterUpgradeableReadLock();
            try
            {
                if (null == s_impl)
                {
                    MakeImplementation();
                }
                return s_impl;
            }
            finally
            {
                s_lock.ExitUpgradeableReadLock();
            }
        }


        // Private Fields
        //======================================================================

        static ReaderWriterLockSlim s_lock;
        static PivotHttpHandlersImpl s_impl;
    }



    /// <summary>
    /// The instance implementation of the handlers.
    /// </summary>
    class PivotHttpHandlersImpl
    {
        public PivotHttpHandlersImpl()
        {
            m_factories = new CollectionFactories();
            m_collectionCache = new CollectionCache();
        }


        // Public Methods
        //======================================================================

        public void AddFactoriesFromFolder(string folderPath)
        {
            m_factories.AddFromFolder(folderPath);
        }

        /// <summary>
        /// Create an HTML fragment listing the available factories.
        /// </summary>
        //TODO: Also return these as a Pivot collection.
        public string CollectionInfoHtml()
        {
            AddDefaultFactoryLocationIfNone();

            StringBuilder text = new StringBuilder();
            text.Append("<div class='PivotFactories'>");

            var sortedFactoriesByName = m_factories.EnumerateFactories().OrderBy(factory => factory.Name);
            foreach (CollectionFactoryBase factory in sortedFactoriesByName)
            {
                text.Append("<div class='PivotFactory'>");
                text.AppendFormat("<div class='PivotFactoryName'>{0}</div>", factory.Name);
                if(!string.IsNullOrEmpty(factory.Summary))
                {
                    //Convert any new-lines into break tags.
                    string htmlSummary = HttpUtility.HtmlEncode(factory.Summary);
                    htmlSummary = htmlSummary.Replace("\n", "<br/>");

                    //TODO: Convert URLs into hyperlinks.

                    text.AppendFormat("<div class='PivotFactorySummary'>{0}</div>", htmlSummary);
                }
                text.Append("<div class='PivotFactorySampleQueries'>");
                if ((null != factory.SampleQueries) && (0 < factory.SampleQueries.Count))
                {
                    foreach (string query in factory.SampleQueries)
                    {
                        string url = string.Format("{0}.cxml?{1}", factory.Name, query);
                        text.AppendFormat("<div class='PivotFactorySampleQuery'><a href='{0}'>{1}</a></div>",
                            url, HttpUtility.HtmlEncode(query));
                    }
                }
                else
                {
                    string url = factory.Name + ".cxml";
                    text.AppendFormat("<div class='PivotFactorySampleQuery'><a href='{0}'>{0}</a></div>", url);
                }
                text.Append("</div>");
                text.Append("</div>");
            }
            text.Append("</div>");

            return text.ToString();
        }

        /// <summary>
        /// Return the list of sample URLs provided by the collection factories.
        /// </summary>
        //TODO: Also return these as a Pivot collection.
        public IEnumerable<string> CollectionSampleUrls()
        {
            AddDefaultFactoryLocationIfNone();

            var sortedFactoriesByName = m_factories.EnumerateFactories().OrderBy(factory => factory.Name);
            foreach (CollectionFactoryBase factory in sortedFactoriesByName)
            {
                if ((null == factory.SampleQueries) || (0 == factory.SampleQueries.Count))
                {
                    yield return (factory.Name + ".cxml");
                }
                else
                {
                    foreach (string query in factory.SampleQueries)
                    {
                        string url = string.Format("{0}.cxml?{1}", factory.Name, query);
                        yield return url;
                    }
                }
            }
        }

        public void ServeCxml(HttpContext context)
        {
            AddDefaultFactoryLocationIfNone();

            string collectionFileName = GetUrlFileBody(context.Request.Url);
            CollectionFactoryBase factory = m_factories.Get(collectionFileName);
            if (null == factory)
            {
                //The requested resource doesn't exist. Return HTTP status code 404.
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Pivot Collection not found.";
                return;
            }

            string pageUrl = string.Empty;
            string requestType = string.Empty;
            string source = string.Empty;
            if (context.Request.QueryString[0]!=null && context.Request.QueryString[0].ToLower() == "datasource")
            {
                requestType = context.Request.QueryString[0].ToLower();//context.Request.QueryString["DataSource"];
            }
            if (context.Request.QueryString[1] != null && context.Request.QueryString[1].ToLower() == "src")
            {
                source = context.Request.QueryString[1].ToLower();//context.Request.QueryString["DataSource"];
            }
            //string source = context.Request.QueryString["src"];
            Collection collection=new Collection();
            if (context.Request.Url.ToString().ToLower().Contains("datasource"))
            {                
                collection = factory.MakeCollection(
                new CollectionRequestContext(context.Request.QueryString, "/" + requestType));               
            }
            else
            {
                collection = factory.MakeCollection(
                    new CollectionRequestContext(context.Request.QueryString, "/" + requestType));
            }
            if (null == collection)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Collection is empty.";
                return;
            }

            ServeCxml(context, collection, collectionFileName);
        }

        public void ServeCxml(HttpContext context, Collection collection, string collectionFileName)
        {
            string collectionKey = collection.SetDynamicDzc(collectionFileName);
            m_collectionCache.Add(collectionKey, collection);

            context.Response.ContentType = "text/xml";
            collection.ToCxml(context.Response.Output);
        }

        public void ServeDzc(HttpContext context)
        {
            string key = GetUrlFileBody(context.Request.Url);
            Collection collection = m_collectionCache.Get(key);
            if (null == collection)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Pivot image data not found. Cache may have expired.";
                return;
            }

            context.Response.ContentType = "text/xml";
            collection.ToDzc(context.Response.Output);
        }

        public void ServeImageTile(HttpContext context)
        {
            ImageRequest request = new ImageRequest(context.Request.Url);

            Collection collection = m_collectionCache.Get(request.DzcName);
            if (null == collection)
            {
                //TODO: Draw this message onto an image tile so it can be seen in Pivot.

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Pivot image not found. Cache may have expired.";
                return;
            }

            CollectionImageTileBuilder builder = new CollectionImageTileBuilder(collection, request,
                DzcSerializer.DefaultMaxLevel, DzcSerializer.DefaultTileDimension);
            builder.Write(context.Response);
        }

        public void ServeDzi(HttpContext context)
        {
            DziRequest request = new DziRequest(context.Request.Url);
            Collection collection = m_collectionCache.Get(request.CollectionKey);
            if (null == collection)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Pivot image data not found. Cache may have expired.";
                return;
            }

            CollectionItem item = collection.Items[request.ItemId];
            ImageProviderBase image = item.ImageProvider;

            context.Response.ContentType = "text/xml";
            DziSerializer.Serialize(context.Response.Output, image.Size);
        }

        public void ServeDeepZoomImage(HttpContext context)
        {
            DeepZoomImageRequest request = new DeepZoomImageRequest(context.Request.Url);

            Collection collection = m_collectionCache.Get(request.CollectionKey);
            if (null == collection)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Pivot image data not found. Cache may have expired.";
                return;
            }

            CollectionItem item = collection.Items[request.ItemId];
            ImageProviderBase image = item.ImageProvider;

            DeepZoomImageTile imageTile = new DeepZoomImageTile(image, request,
                DziSerializer.DefaultTileSize, DziSerializer.DefaultOverlap, DziSerializer.DefaultFormat);
            imageTile.Write(context.Response);
        }


        // Private Methods
        //======================================================================

        private void AddDefaultFactoryLocationIfNone()
        {
            if (0 == m_factories.Count)
            {
                string defaultAssemblyFolder = HttpContext.Current.Server.MapPath("bin");
                AddFactoriesFromFolder(defaultAssemblyFolder);
            }
        }

        private string GetUrlFileBody(Uri url)
        {
            string fileBody = string.Empty;
            if (url.ToString().ToLower().Contains("datasource"))
            {
                fileBody = HttpContext.Current.Request.QueryString[0];
               // fileBody = fileBody.Substring(0, fileBody.LastIndexOf('.'));
                return fileBody;
            }
            else
            {
                string[] pathSegments = url.Segments;
                string fileName = pathSegments[pathSegments.Length - 1];

                //Chop off the extension
                fileBody = fileName.Substring(0, fileName.LastIndexOf('.'));
                return fileBody;
            }
        }


        // Private Fields
        //======================================================================

        CollectionFactories m_factories;
        CollectionCache m_collectionCache;
    }

}
