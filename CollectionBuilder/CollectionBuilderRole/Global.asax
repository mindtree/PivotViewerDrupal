<%@ Application Language="C#" %>
<script runat="server">
   


    void Application_Start(object sender, EventArgs e) 
    {
        // Code that runs on application startup
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Assembly_Resolve);
    }
    
    public string AssemblyFolder
    {
        get { return System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, @"administrator\components\com_hello\views\hellos\tmpl\bin"); }
    }

    void Application_End(object sender, EventArgs e) 
    {
        //  Code that runs on application shutdown
	//PivotHttpHandlers.ApplicationEnd();

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // Code that runs when an unhandled error occurs

    }

    void Session_Start(object sender, EventArgs e) 
    {
        // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e) 
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }

    System.Reflection.Assembly Assembly_Resolve(object sender, ResolveEventArgs args)
    {

        System.Reflection.Assembly assembly = AppDomain.CurrentDomain.Load(System.IO.File.ReadAllBytes(AssemblyFolder + @"\" + args.Name + ".dll"));
        if (args.Name == assembly.FullName)
        {
            return assembly;
        }
        return null;
    } 

</script>




