// Copyright (c) Microsoft Corporation. All rights reserved.

namespace CollectionFactories
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Xml.Linq;
    using PivotServerTools;


    // Twitter API: 
    // http://apiwiki.twitter.com/Twitter-REST-API-Method%3A-statuses-user_timeline
    // e.g. http://api.twitter.com/1/statuses/user_timeline.atom?screen_name=MicrosoftHelps

    public class Twitter : CollectionFactoryBase
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public Twitter()
        {
            this.Summary = "Create a collection of Tweets from a given Twit."
                + "\n[Note, the Twitter API is rate-limited (by the IP address of this server or the web-facing proxy server it goes through), so this server may receive an unauthorized response from Twitter. "
                + "For demo purposes you may save the XML response to a file and pass that to this server instead, e.g. from http://api.twitter.com/1/statuses/user_timeline.atom?screen_name=MicrosoftHelps]"
                + "\nQuery: user=<Twitter-user-name> or url=<url-to-cached-Twitter-atom-file> or topic=<Twitter-hashtag-without-#-symbol>";
            this.SampleQueries = new string[]{
                "user=BillGates"
                ,"user=MicrosoftHelps"
//                ,"url=http://localhost/files/Twitter_user_timeline.atom"
            };
        }


        // Public Methods
        //======================================================================

        public override Collection MakeCollection(CollectionRequestContext context)
        {
            string topic = context.Query["topic"];
            string user = context.Query["user"];
            string fileUrl = context.Query["url"];
            if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(fileUrl) && string.IsNullOrEmpty(topic))
            {
                throw new ArgumentNullException("user", "Include a Twitter user name in the URL as ?user=<name> OR a hashtag as ?topic=<hashtag>");
            }

            string url;
            if (!string.IsNullOrEmpty(user))
            {
                url = string.Format("http://api.twitter.com/1/statuses/user_timeline.atom?screen_name={0}&count=200",
                    HttpUtility.UrlPathEncode(user));
            }
            else if (!string.IsNullOrEmpty(topic))
            {
                if (topic[0] == '#')
                {
                    topic = topic.Remove(0, 1);
                }
                // append a '#' (%23) to the topic (hashtag)
                url = string.Format("http://search.twitter.com/search.atom?q=%23{0}&show_user=true&rpp=100",
                    HttpUtility.UrlPathEncode(topic));
            }
            else
            {
                url = fileUrl;
            }

            string atom;
            try
            {
                using (WebClient web = new WebClient())
                {
                    web.UseDefaultCredentials = true;

                    //TODO: For non-sample use, evaluate whether to use asynchronous methods to release
                    // this web server thread back to the IIS threadpool while waiting for download.
                    // For details, see http://msdn.microsoft.com/en-us/magazine/cc164128.aspx
                    atom = web.DownloadString(url);
                }
            }
            catch(Exception ex)
            {
                return ErrorCollection.FromException(ex);
            }

            Collection collection = CollectionFromAtom(atom);
            return collection;
        }


        // Private Methods
        //======================================================================

        private Collection CollectionFromAtom(string atom)
        {
            XElement root;
            using (StringReader reader = new StringReader(atom))
            {
                root = XElement.Load(reader);
            }

            Collection collection = new Collection();
            MakeCollectionProperties(collection, root);
            foreach (XElement entry in root.Elements(nsAtom + "entry"))
            {
                MakeItem(collection, entry);
            }
            //TODO: Handle continuation data automatically.

            return collection;
        }

        protected virtual void MakeCollectionProperties(Collection collection, XElement root)
        {
            collection.IconUrl = new Uri("http://twitter.com/favicon.ico");
            collection.Name = root.Element(nsAtom + "title").Value;
        }

        protected virtual void MakeItem(Collection collection, XElement entry)
        {
            string title = entry.Element(nsAtom + "title").Value;
            DateTime published = DateTime.Parse(entry.Element(nsAtom + "published").Value);

            string imageUrl = null;
            string alternateUrl = null;
            foreach (var link in entry.Elements(nsAtom + "link"))
            {
                string relType = link.Attribute("rel").Value;
                switch (relType)
                {
                    default:
                        break;

                    case "alternate":
                        alternateUrl = link.Attribute("href").Value;
                        break;

                    case "image":
                        imageUrl = link.Attribute("href").Value;
                        break;
                }
            }

            collection.AddItem(title, alternateUrl, null,
                null //new ItemImage(new Uri(imageUrl))
                ,new Facet("Topics", TopicsInTweet(title))
                ,new Facet("Users", UsersInTweet(title))
                ,new Facet("Published", published)
                ,new Facet("Links", HyperlinksInTweet(title))
                );
        }

        private string[] TopicsInTweet(string title)
        {
            return WordsBeginningWith('#', title);
        }

        private string[] UsersInTweet(string title)
        {
            return WordsBeginningWith('@', title);
        }

        private FacetHyperlink[] HyperlinksInTweet(string title)
        {
            string[] urls = UrlsBeginningWith("http://", title);
            return ((null == urls) || (urls.Length == 0)) ?
                null : urls.Select(s => new FacetHyperlink(s, s)).ToArray();
        }

        private string[] WordsBeginningWith(char startLetter, string text)
        {
            //TODO: Use a RegEx here instead.

            List<string> words = new List<string>();
            int i=0;
            while(i < text.Length)
            {
     	        i = text.IndexOf(startLetter, i);
                if(i < 0)
                {
                    break;
                }

                int endIndex = FindDelimiter(text, i+1);
                string word = text.Substring(i, endIndex - i);
                if (word.Length > 1)
                {
                    words.Add(word);
                }
                i = endIndex + 1;
            }
            return words.Count > 0 ? words.ToArray() : null;
        }

        private int FindDelimiter(string text, int startIndex)
        {
            int index = text.IndexOfAny(delimeters_c, startIndex);
            if(index < 0)
            {
                index = text.Length;
            }
            return index;
        }

        private string[] UrlsBeginningWith(string prefix, string text)
        {
            List<string> words = new List<string>();
            int i = 0;
            while (i < text.Length)
            {
                i = text.IndexOf(prefix, i);
                if (i < 0)
                {
                    break;
                }

                int endIndex = FindUrlEnd(text, i + prefix.Length);
                string word = text.Substring(i, endIndex - i);
                if (word.Length > prefix.Length)
                {
                    words.Add(word);
                }
                i = endIndex + 1;
            }
            return words.Count > 0 ? words.ToArray() : null;
        }

        private int FindUrlEnd(string text, int startIndex)
        {
            int index = text.IndexOfAny(urlDelimeters_c, startIndex);
            if(index < 0)
            {
                index = text.Length;
            }
            return index;
        }


        // Private Fields
        //======================================================================

        static readonly XNamespace nsAtom = "http://www.w3.org/2005/Atom";

        static readonly char[] delimeters_c = { ' ', '.', '.', ':', ';', '?', '!', '-', ')', '\n' };
        static readonly char[] urlDelimeters_c = { ' ', '\n' };
    }

}
