// Copyright (c) Microsoft Corporation. All rights reserved.

namespace CollectionFactories
{
    using System;
    using System.IO;
    using PivotServerTools;
    using System.Web.Hosting;


    /// <summary>
    /// Create a collection from the Shared Pictures folder.
    /// </summary>
    public class SharedPicturesFactory : CollectionFactoryBase
    {
        public SharedPicturesFactory()
        {
            this.Name = "Pictures";
            this.Summary = "A collection of JPG and PNG images from the Windows shared pictures folder on this server.";
        }

        static CollectionRequestContext globalContext;
        public override Collection MakeCollection(CollectionRequestContext context)
        {
            globalContext = context;
            return MakeCollection();
        }

        /// <summary>
        /// Create collection for Pictures 
        /// </summary>
        /// <returns></returns>
        public static Collection MakeCollection()
        {
            string folderName = globalContext.Query["src"];
            string folder = string.Empty;
            try
            {    
                //create collection from folder name passed as query string
                if (!string.IsNullOrEmpty(folderName))
                {
                    folder = HostingEnvironment.ApplicationPhysicalPath + folderName;
                }
                else
                {
                    folder = HostingEnvironment.ApplicationPhysicalPath + @"\Pictures";
                }
                string[] files = Directory.GetFiles(folder);

                Collection coll = new Collection();
                coll.Name = "Pictures from: " + folderName;

                bool anyItems = false;
                foreach (string path in files)
                {
                    string extension = Path.GetExtension(path);
                    bool isJpeg = (0 == string.Compare(".jpg", extension, true));
                    bool isPng = (0 == string.Compare(".png", extension, true));
                    bool isGIF = (0 == string.Compare(".gif", extension, true));
                    bool isTxt = (0 == string.Compare(".txt", extension, true));
                    bool isZip= (0 == string.Compare(".zip", extension, true));
                    bool isRar = (0 == string.Compare(".rar", extension, true));
                    bool isDoc = (0 == string.Compare(".doc", extension, true));
                    bool isDocX = (0 == string.Compare(".docx", extension, true));
                    bool isPPT = (0 == string.Compare(".ppt", extension, true));
                    bool isPPTX = (0 == string.Compare(".pptx", extension, true));
                    bool isXML = (0 == string.Compare(".xml", extension, true));
                    bool isXLSX = (0 == string.Compare(".xlsx", extension, true));
                    FileInfo info = new FileInfo(path);
                    if (isJpeg || isPng || isGIF)
                    {
                        anyItems = true;                        
                        coll.AddItem(Path.GetFileNameWithoutExtension(path), path, null,
                            new ItemImage(path)
                            , new Facet("Content-Type", info.Extension)                                                    
                            , new Facet("File size", info.Length / 1000)
                            , new Facet("Creation time", info.CreationTime)
                            , new Facet("File name", Path.GetFileName(path)
                                , isJpeg ? "*.jpg" : null
                                , isPng ? "*.png" : null
                                , isGIF ? "*.gif" : null
                                )
                            , new Facet("Link:", new FacetHyperlink("click to view image", "/" + folderName + "/" + Path.GetFileName(path)))
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
                                ) 
                            , new Facet("Link:", new FacetHyperlink("click to download file", "/" + folderName + "/" + Path.GetFileName(path)))
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
                    coll.AddItem("No pictures", null,
                        string.Format("The folder \"{0}\" does not contain any Images .", folderName),
                        null);
                }

                return coll;
            }
            catch (Exception)
            {
                Collection tempColl = new Collection();
                tempColl.AddItem("No pictures", null,
                        string.Format("The folder \"{0}\" does not contains any image.", folderName),
                        null);
                return tempColl;
            }
        }
    }
}
