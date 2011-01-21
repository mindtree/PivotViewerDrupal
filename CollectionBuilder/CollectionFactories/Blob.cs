using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PivotServerTools;
using System.IO;
using System.Xml.Linq;
using System.Net;
using System.Xml;
using System.Security.Cryptography;

namespace CollectionFactories
{
    class Blob : CollectionFactoryBase
    {

        public Blob()
        {
                this.Summary = "A collection created by querying the Azure Storage blob container.\n"
                + "[This requires the files to be uploaded to the container.If the client passes the authorization key then all public and private items will be fetched else only the public items will be fetched.]";
        }

        /// <summary>
        /// Create collection from Azure Storage blobs
        /// </summary>
        /// <returns></returns>
        public override Collection MakeCollection(CollectionRequestContext context)
        {
            try
            {
                Collection collection = new Collection();
                collection.Name = "Azure Blob Storage collection";
                //Check for null values
                if (string.IsNullOrEmpty(context.Query["AccountName"]) || string.IsNullOrEmpty(context.Query["ContainerName"]))
                {
                    throw new ArgumentNullException("Either the Account Name or Container Name passed is Empty!");
                }
                //Consruct rest URL
                string url = string.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list",context.Query["AccountName"],context.Query["ContainerName"]);
               
                string authorizationkey = string.Empty;
                string dateTime=string.Empty;
                
                string startTime=context.Query["StartTime"];
                string endTime=context.Query["EndTime"];
               
                string blobAuthKey=context.Query["BlobAuthKey"];
                //Get Authorization key from Query if present
                if (!string.IsNullOrEmpty(context.Query["SecretKey"]))
                {
                    authorizationkey = "SharedKey " + context.Query["AccountName"] + ":" + context.Query["SecretKey"].ToString();
                    dateTime = context.Query["DateTime"].ToString();
                }
                //Get web response
                string response = getWebResponse(url, authorizationkey,dateTime);
                TextReader containerXML = new StringReader(response);
                XDocument xdocResponse = XDocument.Load(containerXML);
                //Extract blob list from Response XML
                var blobList = (from c in xdocResponse.Descendants()
                                where c.Name == "Blob"
                                select c);
                string blobName = "";
                string blobURL = "";
                string contentType = "";
                int contentLength;
                string lastModified = "";
                XElement blobNameElement = null;
                XElement blobURLElement = null;
                XElement contentTypeElement = null;
                XElement contentLengthElement = null;
                XElement lastModifiedElement = null;
                ItemImage image = null;
                string extension = "";
                string fileName = "";
                 bool isPrivateBlob;

                foreach (XElement blob in blobList)
                {

                    //obtain blob attributes
                    blobNameElement = (from c in blob.Descendants()
                                       where c.Name == BlobXMLNodes.blobName
                                       select c).FirstOrDefault();
                    if (blobNameElement != null)
                    {
                        blobName = blobNameElement.Value;
                    }
                    else
                    {
                        blobName = "";
                    }

                    blobURLElement = (from c in blob.Descendants()
                               where c.Name == BlobXMLNodes.blobURL
                               select c).FirstOrDefault();

                    if (blobURLElement != null)
                    {
                        blobURL = blobURLElement.Value;
                    }
                    else
                    {
                        blobURL = "";
                    }
                    isPrivateBlob = false;
                    if (string.IsNullOrEmpty(context.Query["SecretKey"]))
                    {

                        //if blob is public(i.e authkey not present)

                        contentTypeElement = (from c in blob.Descendants()
                                       where c.Name == BlobXMLNodes.publicBlobType
                                       select c).FirstOrDefault();
                       
                        lastModifiedElement = (from c in blob.Descendants()
                                        where c.Name == BlobXMLNodes.publicBlobLastModified 
                                        select c).FirstOrDefault();
                       

                        contentLengthElement = (from c in blob.Descendants()
                                                         where c.Name == BlobXMLNodes.publicBlobSize 
                                                         select c).FirstOrDefault();
                       
                    }
                    else
                    {
                        //private blobs
                        isPrivateBlob = true;

                        contentTypeElement = (from c in blob.Descendants()
                                       where c.Name == BlobXMLNodes.privateBlobType
                                       select c).FirstOrDefault();
                        lastModifiedElement = (from c in blob.Descendants()
                                        where c.Name == BlobXMLNodes.privateBlobLastModified
                                        select c).FirstOrDefault();
                        contentLengthElement = (from c in blob.Descendants()
                                                         where c.Name == BlobXMLNodes.privateBlobSize
                                                         select c).FirstOrDefault();
                    }
                    if (contentTypeElement != null)
                    {
                        contentType = contentTypeElement.Value;
                    }
                    else
                    {
                        contentType = "";
                    }
                    if (lastModifiedElement != null)
                    {
                        lastModified = lastModifiedElement.Value;
                    }
                    else
                    {
                        lastModified = "";
                    }
                    if (contentLengthElement != null)
                    {
                        contentLength = Convert.ToInt32(contentLengthElement.Value);
                    }
                    else
                    {
                        contentLength = 0;
                    }

                    //extract file extension and file name from blob URL
                    extension = Path.GetExtension(blobURL);
                    fileName = Path.GetFileName(blobURL);

                    //Append shared key to blob URL along with the start time and end time if the blob is private
                    if (isPrivateBlob)
                    {
                       blobURL+= string.Format("?st={0}&se={1}&sr=c&sp=r&sig={2}", startTime, endTime, blobAuthKey);
                    }
                    image = new ItemImage();
                    image.ImageUrl = new Uri(blobURL);

                    //For 2nd level filtering based on extension
                    bool isJpg = (0 == string.Compare(".jpg", extension, true));
                    bool isJpeg = (0 == string.Compare(".jpeg", extension, true));
                    bool isPng = (0 == string.Compare(".png", extension, true));
                    bool isTif = (0 == string.Compare(".tif", extension, true));
                    bool isGIF = (0 == string.Compare(".gif", extension, true));
                    bool isTxt = (0 == string.Compare(".txt", extension, true));
                    bool isZip = (0 == string.Compare(".zip", extension, true));
                    bool isRar = (0 == string.Compare(".rar", extension, true));
                    bool isDoc = (0 == string.Compare(".doc", extension, true));
                    bool isDocX = (0 == string.Compare(".docx", extension, true));
                    bool isPPT = (0 == string.Compare(".ppt", extension, true));
                    bool isPPTX = (0 == string.Compare(".pptx", extension, true));
                    bool isXML = (0 == string.Compare(".xml", extension, true));
                    bool isXLSX = (0 == string.Compare(".xlsx", extension, true));
                    bool isTAR = (0 == string.Compare(".tar", extension, true));
                    bool isGZ = (0 == string.Compare(".gz", extension, true));                   

                   
                    //If image type blob then add Image to collection
                    if ((blobName.ToLower().Contains(".jpg") || blobName.ToLower().Contains(".jpeg") ||
                        blobName.ToLower().Contains(".tif") || blobName.ToLower().Contains(".gif") ))
                   {                       

                        collection.AddItem(blobName, null, blobName,image
                            , new Facet("Content-Type", contentType)
                            , new Facet("Content-Length", contentLength)
                            , new Facet("Last-Modified", Convert.ToDateTime(lastModified))
                            , new Facet("File name", fileName
                                , isJpg ? "*.jpg" : null
                                , isPng ? "*.png" : null
                                , isJpeg ? "*.jpeg" : null
                                , isGIF ? "*.gif" : null
                                , isTif ? "*.tif" : null                                 
                                )
                            , new Facet("Link:", new FacetHyperlink("click to view", blobURL))
                            );
                    }
                    else
                    {
                        collection.AddItem(blobName, null, null, null
                            , new Facet("Content-Type", contentType)
                            , new Facet("Content-Length", contentLength)
                            , new Facet("Last-Modified", Convert.ToDateTime(lastModified))
                            , new Facet("File name", fileName
                                , isTxt ? "*.txt" : null
                                , isZip ? "*.zip" : null
                                , isRar ? "*.rar" : null
                                , isDoc ? "*.doc" : null
                                , isDocX ? "*.docx" : null
                                , isPPT ? "*.ppt" : null
                                , isPPTX ? "*.pptx" : null
                                , isXML ? "*.xml" : null
                                , isXLSX ? "*.xslx" : null
                                , isTAR ? "*.tar" : null
                                , isGZ ? "*.gz" : null 
                                )
                            , new Facet("Link:", new FacetHyperlink("click to view", blobURL))
                            );
                    }
                }

                return collection;
            }
            catch (WebException wex)
            {
                Collection collection = new Collection();
                collection.AddItem(null, null, "Azure server did not respond.Please check the credentials and try again."
                           , null
                           );
                return collection;
            }
            catch (Exception ex)
            {
                Collection collection = new Collection();
                collection.AddItem(null, null, "Unable to connect to Azure blob storage using provided credential"
                           , null
                           );
                return collection;
            }

        }


        

        /// <summary>
        /// This method performs a HTTP Get to get blob items from a container
        /// </summary>
        /// <param name="uri">Request URI</param>
        /// <param name="authorizationKey">Authorization Key to Fetch Private blob items</param>
        /// <returns>Web response</returns>
        private string getWebResponse(string uri, string authorizationKey,string dateTime)
        {
            HttpWebRequest request;
            string xml;

            request = (HttpWebRequest)WebRequest.Create(uri);
            if (!string.IsNullOrEmpty(authorizationKey))
            {
                request.Headers.Add("x-ms-date", dateTime);
                request.Headers.Add("Authorization", authorizationKey);
            }
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (System.IO.StreamReader responseStream = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        //return the response XML
                        xml = responseStream.ReadToEnd();
                        return xml;
                    }
                }
            }
            catch (WebException wex)
            {
                throw wex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
    internal class BlobXMLNodes
    {

        public const string blobName = "Name";
        public const string blobURL = "Url";
        public const string publicBlobType = "Content-Type";
        public const string publicBlobLastModified = "Last-Modified";
        public const string publicBlobSize = "Content-Length";
        public const string privateBlobType = "ContentType";
        public const string privateBlobLastModified = "LastModified";
        public const string privateBlobSize = "Size";

    }
}
