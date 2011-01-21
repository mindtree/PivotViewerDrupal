using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PivotServerTools;
using CollectionBuilderRole;
using System.Security.Cryptography;

namespace CollectionBuilderRole
{
    /// <summary>
    /// Summary description for GenericRequestHandler
    /// </summary>
    public class GenericRequestHandler : IHttpHandler
    {

        
        /// <summary>
        /// Handle request for this Handler based on HttpContext
        /// </summary>
        /// <param name="context">HttpContext</param>
        public void ProcessRequest(HttpContext context)
        {
            string file = string.Empty;            
            if (context.Request.QueryString.Keys.Count > 0)
            {
                if (context.Request.QueryString.AllKeys != null && context.Request.QueryString.AllKeys[0]!=null)
                {
                    if (context.Request.QueryString.AllKeys[0].ToLower() == Constants.DataSource)
                    {
                        file = context.Request.QueryString[0];
                    }
                    if (!string.IsNullOrEmpty(file))
                    {                        
                       PivotHttpHandlers.ServeCxml(context);                        
                    }
                }
            }
        }

        /// <summary>
        /// Enable/Disable call to handler again for same url
        /// </summary>
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}