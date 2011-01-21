// Copyright (c) Microsoft Corporation. All rights reserved.

namespace CollectionFactories
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Principal;
    using System.Web;
    using System.Xml.Linq;
    using PivotServerTools;


    /// <summary>
    /// Create a collection from OData feeds.
    /// See: http://www.odata.org/developers/protocols/atom-format
    /// </summary>
    public class ODataCollectionSource : CollectionFactoryBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public ODataCollectionSource()
        {
            this.Name = "OData";
            this.Summary = "Create a collection from an OData Atom feed or AtomPub resource. Double-click an item in an AtomPub collection to open the child Atom feed. "
                + "Text on the items comes from [entry/properties/Name] if present, otherwise [entry/title]. In this general-purpose demo, facets are created for every [entry/property] element, but in practice you would choose a smaller set.\n"
                + "Query: src=<URL-of-OData-feed>";
            this.SampleQueries = new string[]{
                "src=http://odata.netflix.com/Catalog"
                ,"src=http://odata.netflix.com/Catalog/Titles?$select=AverageRating,ReleaseYear,Rating"
                ,"src=http://api.visitmix.com/OData.svc/Sessions"
                ,"src=http://api.visitmix.com/OData.svc/Speakers"
                ,"src=https://odata.sqlazurelabs.com/OData.svc/v0.1/hqd7p8y6cy/Northwind"
            };
        }


        // Interface and base class realizations
        //======================================================================

        public override Collection MakeCollection(CollectionRequestContext context)
        {
            return MakeOdataCollection(context);
        }


        // Public Methods
        //======================================================================

        public static Collection MakeOdataCollection(CollectionRequestContext context)
        {
            string odataUrl = context.Query["src"];

            ODataFeedBuilder builder = new ODataFeedBuilder(odataUrl, context.Url);
            return builder.GetCollection();
        }

    }


    internal static class ODataXmlns
    {
        public static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
        public static readonly XNamespace Data = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        public static readonly XNamespace Metadata = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
    }


    internal class ODataFeedBuilder
    {
        // Public static methods
        //======================================================================


        // Constructors, Finalizer and Dispose
        //======================================================================

        public ODataFeedBuilder(string odataUrl, string collectionBaseUrl)
        {
            m_odataUrl = odataUrl;
            m_collectionBaseUrl = collectionBaseUrl;
        }


        // Interface and base class realizations
        //======================================================================


        // Public Methods
        //======================================================================

        public Collection GetCollection()
        {
            try
            {
                m_root = DownloadOdata(m_odataUrl);
            }
            catch (Exception ex)
            {
                return ErrorCollection.FromException(ex);
            }

            if (IsAtomPub(m_root))
            {
                return new AtomPub(m_root, m_collectionBaseUrl).MakeCollection();
            }
            else if (IsAtom(m_root))
            {
                return MakeCollectionFromFeed(m_root);
            }

            //TODO: also support just an <entry> element as the root.
            throw new ApplicationException("Root element is not <feed> or <service>");
        }


        // Public Properties
        //======================================================================


        // Private Methods
        //======================================================================

        private static XElement DownloadOdata(string url)
        {
            string odata;

            using (ImpersonateCaller())
            using (WebClient client = new WebClient())
            {
                client.UseDefaultCredentials = true;

                //TODO: For non-sample use, evaluate whether to use asynchronous methods to release
                // this web server thread back to the IIS threadpool while waiting for download.
                // For details, see http://msdn.microsoft.com/en-us/magazine/cc164128.aspx
                odata = client.DownloadString(url);
            }

            using (StringReader reader = new StringReader(odata))
            {
                return XElement.Load(reader);
            }
        }

        private static IDisposable ImpersonateCaller()
        {
            //For internal sites, e.g. Sharepoint 2010, we need to authenticate using the caller's identity
            // and also specify useDefaultCredentials in order to download data from the site.
            // Therefore in IIS, "Anonymous Authentication" must be disabled and "Windows Authentication" enabled.
            //However, the Pivot client claims "unauthorized" against IIS if Anonymous is not on.
            // The Silverlight control works correctly if only Windows authentication is on.

            IIdentity runningUser = HttpContext.Current.User.Identity;
            return runningUser.IsAuthenticated ? ((WindowsIdentity)runningUser).Impersonate() : null;
        }


        private static bool IsAtomPub(XElement root)
        {
            return "service" == root.Name.LocalName;
        }

        private static bool IsAtom(XElement root)
        {
            return "feed" == root.Name.LocalName;
        }

        private Collection MakeCollectionFromFeed(XElement root)
        {
            const int countMaxIterations_c = 4;
            const int minItemCount = 100;

            Collection collection = new Collection();
            collection.Name = root.Element(ODataXmlns.Atom + "title").Value;
            //collection.ImgBaseName = string.Empty;

            int countItems = AddEntriesToCollection(collection, root);

            //See if there is continuation data, but only use it if we don't have enough items yet.
            for (int i = 0; (i < countMaxIterations_c) && (countItems < minItemCount); ++i)
            {
                string href = GetLinkRelNext(root);
                if (string.IsNullOrEmpty(href))
                {
                    break;
                }

                //TODO: Load these ahead of time, asynchronously.
                root = DownloadOdata(href);
                countItems += AddEntriesToCollection(collection, root);
            }

            return collection;
        }

        private int AddEntriesToCollection(Collection collection, XElement root)
        {
            int countItems = 0;
            foreach (XElement element in root.Elements(ODataXmlns.Atom + "entry"))
            {
                ODataCollectionItem item = new ODataCollectionItem(element, m_collectionBaseUrl, m_odataUrl);
                collection.AddItem(item.Name, item.Url, item.Description, item.Image, item.Facets);
                ++countItems;
            }
            return countItems;
        }

        private static string GetLinkRelNext(XElement root)
        {
            XElement linkRelNext = root.Elements(ODataXmlns.Atom + "link")
                                       .Where(link => ("next" == link.Attribute("rel").Value))
                                       .FirstOrDefault();
            return (null == linkRelNext) ? null : linkRelNext.Attribute("href").Value;
        }


        // Private Fields
        //======================================================================

        string m_odataUrl;
        string m_collectionBaseUrl;
        XElement m_root;
    }


    /// <summary>
    /// A collection item created from an OData entry.
    /// </summary>
    internal class ODataCollectionItem
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public ODataCollectionItem(XElement entry, string collectionBaseUrl, string odataUrl)
        {
            m_collectionBaseUrl = collectionBaseUrl;
            m_odataUrl = odataUrl;

            MakeFromEntry(entry);
        }


        // Public Properties
        //======================================================================

        public string Name { get; private set; }
        public string Url { get; private set; }
        public ItemImage Image { get; private set; }
        public string Description { get; private set; }
        public Facet[] Facets
        {
            get;
            private set;
        }


        // Private Methods
        //======================================================================

        private void MakeFromEntry(XElement entry)
        {
            this.Name = entry.Element(ODataXmlns.Atom + "title").Value;
            var propertiesElement = FindPropertiesElement(entry);
            var nameElement = propertiesElement.Element(ODataXmlns.Data + "Name");
            if (null != nameElement)
            {
                this.Name = nameElement.Value;
            }

            CheckIfMediaLinkEntry(entry);

            List<Facet> facets = new List<Facet>();
            facets.AddRange(EntryProperties(entry));
            facets.AddRange(EntryLinks(entry));

            this.Facets = facets.ToArray();
        }

        private static XElement FindPropertiesElement(XElement element)
        {
            var propertiesElements = element.Descendants(ODataXmlns.Metadata + "properties");
            if (null != propertiesElements)
            {
                return propertiesElements.FirstOrDefault();
            }
            return null;
        }

        private bool CheckIfMediaLinkEntry(XElement entry)
        {
            XElement content = entry.Element(ODataXmlns.Atom + "content");
            XAttribute typeAttribute = content.Attribute("type");
            if (null != typeAttribute)
            {
                if (IsImageMimeType(typeAttribute.Value))
                {
                    string src = content.Attribute("src").Value;
                    //TODO: Handle relative URLs as well
                    if(Uri.IsWellFormedUriString(src, UriKind.Absolute))
                    {
                        this.Image = new ItemImage(new Uri(src));
                        return true;
                    }
                }
            }
            return false;
        }

        static readonly string[] imageMimeTypes_c = { "image/jpeg", "image/png" };
        private bool IsImageMimeType(string mimeType)
        {
            return imageMimeTypes_c.Contains(mimeType, StringComparer.InvariantCultureIgnoreCase);
        }

        private static IEnumerable<Facet> EntryProperties(XElement entry)
        {
            var propertiesElement = FindPropertiesElement(entry);
            if (null == propertiesElement)
            {
                yield break;
            }

            foreach (var property in propertiesElement.Descendants())
            {
                //TODO:Handle the property having child nodes.

                Facet facet = FacetFromProperty(property, entry);
                if (null != facet)
                {
                    yield return facet;
                }
            }
        }

        //TODO: Make this overridable by derived classes, or have a way to provide an alternate implementation.
        private static Facet FacetFromProperty(XElement property, XElement parentEntry)
        {
            string category = property.Name.LocalName;
            if (Facet.IsReservedCategory(category))
            {
                category += "_";
            }

            FacetType facetType = FacetType.Text;
            object value = null;
            try
            {
                value = PivotFacetFromOdataProperty(property, out facetType);
            }
            catch
            {
                var id = parentEntry.Element(ODataXmlns.Atom + "id");
                string idValue = (null == id) ? string.Empty : id.Value;

                Console.Error.WriteLine("id \"{0}\", property {1} contains bad value \"{2}\"",
                    idValue, category, property.Value);
            }

            return (null == value) ? null : new Facet(category, facetType, value);
        }

        private static object PivotFacetFromOdataProperty(XElement prop, out FacetType facetType)
        {
            string textValue = prop.Value;

            XAttribute typeAttr = prop.Attribute(ODataXmlns.Metadata + "type");
            string attr = (null == typeAttr) ? null : typeAttr.Value;

            switch (attr)
            {
                default:
                    //If text looks like a URL, make this a hyperlink.
                    if (textValue.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
                    {
                        facetType = FacetType.Link;
                        return new FacetHyperlink(textValue, textValue);
                    }

                    facetType = FacetType.Text;
                    if (string.IsNullOrEmpty(textValue))
                    {
                        return null;
                    }
                    return textValue;

                case "Edm.Int32":
                    facetType = FacetType.Number;
                    if (string.IsNullOrEmpty(textValue))
                    {
                        return null;
                    }
                    return int.Parse(textValue);

                case "Edm.DateTime":
                    facetType = FacetType.DateTime;
                    if (string.IsNullOrEmpty(textValue))
                    {
                        return null;
                    }
                    return DateTime.Parse(textValue);

                //TODO: Other Edm types
            }
        }

        private IEnumerable<Facet> EntryLinks(XElement entry)
        {
            //Get the links from the entry
            List<FacetHyperlink> links = new List<FacetHyperlink>();
            foreach (var link in entry.Elements(ODataXmlns.Atom + "link"))
            {
                FacetHyperlink facetHyperlink = MakeFacetHyperlink(link);
                links.Add(facetHyperlink);
            }
            if (links.Count > 0)
            {
                yield return new Facet("Links:", links.ToArray());
            }
        }

        private FacetHyperlink MakeFacetHyperlink(XElement link)
        {
            string title = link.Attribute("title").Value;
            string href = link.Attribute("href").Value;
            string relation = link.Attribute("rel").Value;

/*
            if (0 == string.Compare(relation, "edit-media", true))
            {
                // This is an OData "Media Link Entry": 
                // http://www.odata.org/developers/protocols/atom-format#RepresentingMediaLinkEntries
                // <atom:content src="{MediaResourceUri}" type="{MimeType}">
                //Use the content as the item.
                string url = href;
            }
*/

            //if the relation is human readable (not in URI form), append it to the title to distinguish the title.
            if (!relation.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
            {
                title = string.Format("{0} ({1})", title, relation);
            }

            if (!href.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
            {
                //TODO: If it has $value, replace that with the parameter.

                //Add the base url.
                href = AtomPub.MakeUrl(m_collectionBaseUrl, m_odataUrl, href);
            }

            FacetHyperlink facetHyperlink = new FacetHyperlink(title, href);
            return facetHyperlink;
        }


        // Private Fields
        //======================================================================

        string m_odataUrl;
        string m_collectionBaseUrl;
    }



    internal class AtomPub
    {
        public AtomPub(XElement root, string urlPivotServer)
        {
            m_root = root;
            m_urlPivotServer = urlPivotServer;
        }

        public Collection MakeCollection()
        {
            ReadRoot();

            Collection collection = new Collection();
            collection.Name = m_baseUrl;
            //collection.ImgBaseName = string.Empty;

            //collection.HrefBase = GetHrefBase();

            foreach (XElement workspace in m_root.Elements(nsApp + "workspace"))
            {
                string workSpaceTitle = GetAtomTitle(workspace);

                foreach (XElement coll in workspace.Elements(nsApp + "collection"))
                {
                    string href = coll.Attribute("href").Value;
                    string title = GetAtomTitle(coll);

                    collection.AddItem(title, FormatHref(href), null, null);
                }
            }

            return collection;
        }


        // Private Methods
        //======================================================================

        private void ReadRoot()
        {
            if (0 != string.Compare("service", m_root.Name.LocalName, true))
            {
                throw new ApplicationException("Root element must be \"service\"");
            }

            ReadRootBaseAttribute();
        }

        private void ReadRootBaseAttribute()
        {
            XAttribute attrBase = m_root.Attribute(XNamespace.Xml + "base");
            if (null != attrBase)
            {
                m_baseUrl = attrBase.Value;
            }
        }

        private string GetHrefBase()
        {
            return string.Format("{0}?src={1}", m_urlPivotServer, m_baseUrl);
        }

        private string GetAtomTitle(XElement element)
        {
            return element.Element(nsAtom + "title").Value;
        }

        private string FormatHref(string href)
        {
            return MakeUrl(m_urlPivotServer, m_baseUrl, href);
        }

        public static string MakeUrl(string urlPivotServer, string urlOdataBase, string relativeHref)
        {
            return string.Format("{0}?src={1}{2}", urlPivotServer, urlOdataBase, relativeHref);
        }


        // Private Fields
        //======================================================================

        static readonly XNamespace nsAtom = "http://www.w3.org/2005/Atom";
        static readonly XNamespace nsApp = "http://www.w3.org/2007/app";

        XElement m_root;
        string m_urlPivotServer;
        string m_baseUrl;
    }

}
