// Copyright (c) Microsoft Corporation. All rights reserved.

namespace CollectionFactories
{
    using System;
    using System.Linq;
    using PivotServerTools;


    // Steps needed prior to using this class:
    //
    // Install SQL Server 2008 or SQL Server 2008 R2.
    // Download and install the appropriate AdventureWorks sample database from http://msftdbprodsamples.codeplex.com/
    // View > Server Explorer.
    //     Connect to Database.
    //         Server name = localhost
    //         Database = AdventureWorks2008 (or AdventureWorks2008R2)
    // Right-click "Collection Sources" in Solution Explorer window.
    //     Add > New Item...
    //     Choose "LINQ to SQL classes".
    //     Name = SqlAdventureWorks.dbml
    // Drag database Tables from Server Explorer into SqlAdventureWorks.dbml window: Product, ProductPhoto, ProductProductPhoto
    //
    public class SqlAdventureWorks : CollectionFactoryBase 
    {
        // Constructors, Finalizer and Dispose
        //======================================================================

        public SqlAdventureWorks()
        {
            this.Summary = "A collection created by querying the Products table of the AdventureWorks sample database for SQL Server 2008 R2.\n"
                + "[This requires the AdventureWorks2008R2 database to be installed on this web server and the ASP.NET account (e.g. IIS APPPOOL\\ASP.NET v4.0) to have a login to the database server, with a user mapping to the AdventureWorks2008R2 database and db_datareader access.]";
        }


        // Public Methods
        //======================================================================

        public override Collection MakeCollection(CollectionRequestContext context)
        {
            const int maxItems_c = 150;

            try
            {
                var products = from p in m_dataContext.Products
                               where p.ListPrice > 0
                               select p;

                Collection collection = new Collection();
                collection.Name = "Adventure Works database";

                foreach (var product in products.Take(maxItems_c))
                {
                    //TODO: An exercise for the reader:
                    // Load the image data from the ProductPhotos database table and use it to draw the item.
                    ItemImage image = null;

                    collection.AddItem(product.Name, null, null, image
                        , new Facet("Class", product.Class)
                        , new Facet("Color", product.Color)
                        , new Facet("List price", (double)product.ListPrice)
                        );
                }
                collection.SetFacetFormat("List price", "$#,0.00");
                return collection;
            }
            catch (Exception ex)
            {
                return ErrorCollection.FromException(ex);
            }
        }


        // Private Fields
        //======================================================================

        SqlAdventureWorksDataContext m_dataContext = new SqlAdventureWorksDataContext();
    }

}
