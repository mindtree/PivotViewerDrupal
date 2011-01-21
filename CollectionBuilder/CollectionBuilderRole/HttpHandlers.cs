// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Web;
using PivotServerTools;

namespace CollectionBuilderRole
{

    public class TestHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.Write("Test Handler called");
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }

    public class DzcHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            PivotHttpHandlers.ServeDzc(context);
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }


    public class ImageTileHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            PivotHttpHandlers.ServeImageTile(context);
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }


    public class DziHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            PivotHttpHandlers.ServeDzi(context);
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }


    public class DeepZoomImageHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            PivotHttpHandlers.ServeDeepZoomImage(context);
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }

}
