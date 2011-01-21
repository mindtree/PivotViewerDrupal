<%@Page Language="C#" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
	    //Load the assemblies from the specified path. 					
        System.Reflection.Assembly.LoadFrom(Server.MapPath("bin\\CollectionBuilderRole.dll"));
        System.Reflection.Assembly.LoadFrom(Server.MapPath("bin\\CollectionFactories.dll"));
        System.Reflection.Assembly.LoadFrom(Server.MapPath("bin\\Microsoft.WindowsAzure.Diagnostics.dll"));
        System.Reflection.Assembly.LoadFrom(Server.MapPath("bin\\Microsoft.WindowsAzure.StorageClient.dll"));
        var pivot = System.Reflection.Assembly.LoadFrom(Server.MapPath("bin\\PivotServerTools.dll"));

        AppDomain.CurrentDomain.AppendPrivatePath(Server.MapPath("bin"));

        HttpContext context = HttpContext.Current;
        string file = string.Empty;
        if (context.Request.QueryString.Keys.Count > 0)
        {
            if (context.Request.QueryString.AllKeys != null && context.Request.QueryString.AllKeys[0] != null)
            {
                if (context.Request.QueryString.AllKeys[0].ToLower() == "datasource")
                {
                    file = context.Request.QueryString[0];
                }
                if (!string.IsNullOrEmpty(file))
                {
                    var handlers = pivot.GetType("PivotServerTools.PivotHttpHandlers");
                    var method = handlers.GetMethod("ServeCxml", new Type[] { typeof(System.Web.HttpContext) });                        
                    method.Invoke(null, new object[] {context});
                }
            }
        }		
    } 
</script>

