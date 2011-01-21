// Copyright (c) Microsoft Corporation. All rights reserved.

namespace CollectionFactories
{
    using System;
    using System.IO;
    using PivotServerTools;
    using System.Web.Hosting;
    using System.Security.Cryptography;
    using System.Web;


    /// <summary>
    /// Create a collection from the Shared Pictures folder.
    /// </summary>
    public class ServerFolder : CollectionFactoryBase
    {
        public static HMACSHA256 HashedKey { get; set; }
        public ServerFolder()
        {

            this.Name = "ServerFolder";
            this.Summary = "A collection of assets in folder on this server.";
        }

        static CollectionRequestContext globalContext;
        public override Collection MakeCollection(CollectionRequestContext context)
        {
            globalContext = context;
            return MakeCollection();
        }

        /// <summary>
        /// Create collection from files in a server folder 
        /// </summary>
        /// <returns></returns>
        public static Collection MakeCollection()
        {
            string folderPath = globalContext.Query["FolderPath"];
            UriBuilder folderUri=new UriBuilder();
            Uri hostAndPort = new Uri(System.Web.HttpContext.Current.Request.Url.Scheme + Uri.SchemeDelimiter + HttpContext.Current.Request.ServerVariables["HTTP_HOST"]);
            //For Recognizing if the folder path is Absolute or Relative
            bool isRelative = false;

            string folder = string.Empty;
            try
            {
                //create collection from folder name passed as query string
                if (!string.IsNullOrEmpty(folderPath))
                {

                    folder = HttpUtility.UrlDecode(folderPath);
                    //If the folder name passed does not contain a :, we assume the user is refering to a relative path
                    if (folder.IndexOf(":") < 0)
                    {
                      
                        folder = HostingEnvironment.ApplicationPhysicalPath + folder;
                        //Construct the base URL
                        folderUri.Scheme = System.Web.HttpContext.Current.Request.Url.Scheme;
                        folderUri.Host = hostAndPort.Host;
                        folderUri.Port = hostAndPort.Port;
                        folderUri.Path = HttpUtility.UrlDecode(folderPath);
                        isRelative = true;
                        
                    }
                }
                else
                {
                    //if folder name is empty we look for a folder called pictures on the server
                  
                    folder = HostingEnvironment.ApplicationPhysicalPath + "Pictures";
                    folderUri.Scheme = System.Web.HttpContext.Current.Request.Url.Scheme;
                    folderUri.Host = hostAndPort.Host;
                    folderUri.Port = hostAndPort.Port;
                    folderUri.Path = "Pictures";
                    isRelative = true;
                }
                //Get Folder files
                string[] files = Directory.GetFiles(folder);

                Collection coll = new Collection();
                coll.Name = "Pictures from: " + folderPath;

                bool anyItems = false;
                foreach (string path in files)
                {
                    string extension = Path.GetExtension(path);
                    //For second level filtering on extension
                    bool isJpg = (0 == string.Compare(".jpg", extension, true));
                    bool isJpeg = (0 == string.Compare(".jpeg", extension, true));
                    bool isTif = (0 == string.Compare(".tif", extension, true));                    
                    bool isPng = (0 == string.Compare(".png", extension, true));
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
                    FileInfo info = new FileInfo(path);
                    if (isJpeg || isPng || isGIF || isTif|| isJpg)
                    {
                        anyItems = true;
                        coll.AddItem(Path.GetFileNameWithoutExtension(path), path, null,
                            new ItemImage(path)
                            , new Facet("Content-Type", info.Extension)
                            , new Facet("File size", info.Length / 1000)
                            , new Facet("Creation time", info.CreationTime)
                            , new Facet("File name", Path.GetFileName(path)
                                , isJpg ? "*.jpg" : null
                                , isJpeg ? "*.jpeg" : null
                                , isPng ? "*.png" : null
                                , isGIF ? "*.gif" : null
                                , isTif ? "*.tif" : null
                                )

                              , isRelative ? new Facet("Link:", new FacetHyperlink("click to view image", folderUri.ToString() + "/" + Path.GetFileName(path))) : new Facet("Link:", "")
                            );
                    }
                    else
                    {
                        coll.AddItem(Path.GetFileNameWithoutExtension(path), path, null,
                           null
                            , new Facet("Content-Type", info.Extension)
                            , new Facet("File size", info.Length / 1000)
                            , new Facet("Creation time", info.CreationTime)
                            , new Facet("File name", Path.GetFileName(path)
                                , isTxt ? "*.txt" : null
                                , isZip ? "*.zip" : null
                                , isRar ? "*.rar" : null
                                , isDoc ? "*.doc" : null
                                , isDocX ? "*.docx" : null
                                , isPPT ? "*.ppt" : null
                                , isPPTX ? "*.pptx" : null
                                , isXML ? "*.xml" : null
                                , isXLSX ? "*.xlsx" : null
                                , isTAR ? "*.tar" : null
                                , isGZ ? "*.gz" : null 
                                )
                            , isRelative ? new Facet("Link:", new FacetHyperlink("click to view image", folderUri.ToString() + "/" + Path.GetFileName(path))) : new Facet("Link:", "")
                            );
                       
                    }
                }

                if (anyItems)
                {
                    coll.SetFacetDisplay("Creation time", false, true, false);
                    coll.SetFacetFormat("File size", "#,#0 kb");
                }
                else
                {
                    coll.AddItem("No files", null,
                        string.Format("The folder \"{0}\" does not contain any files .", folder),
                        null);
                }

                return coll;
            }
            catch (Exception)
            {
                Collection tempColl = new Collection();
                tempColl.AddItem("No files", null,
                        string.Format("The folder \"{0}\" does not contain any files.", folder),
                        null);
                return tempColl;
            }
        }
    }
}
