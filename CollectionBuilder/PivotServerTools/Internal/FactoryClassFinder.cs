// Copyright (c) Microsoft Corporation. All rights reserved.

namespace PivotServerTools.Internal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;


    /// <summary>
    /// Methods to locate assemblies and classes that implementat CollectionFactoryBase
    /// </summary>
    internal static class FactoryClassFinder
    {
        // Public Methods
        //======================================================================

        /// <summary>
        /// Return new instances of all classes deriving from CollectionFactoryBase
        /// from all assemblies in the given folder.
        /// </summary>
        public static IEnumerable<CollectionFactoryBase> Find(string folderPath)
        {
            List<CollectionFactoryBase> factories = new List<CollectionFactoryBase>();

            foreach (Type t in EnumerateTypesInAssemblies(folderPath))
            {
                if (t.IsSubclassOf(typeof(CollectionFactoryBase)))
                {
                    CollectionFactoryBase factory = (CollectionFactoryBase)Activator.CreateInstance(t);
                    if (string.IsNullOrEmpty(factory.Name))
                    {
                        factory.Name = t.Name;
                    }

                    factories.Add(factory);
                }
            }
            return factories;
        }


        // Private Methods
        //======================================================================

        private static IEnumerable<Assembly> EnumerateAssemblies(string folderPath)
        {
            string[] dllFilePaths = Directory.GetFiles(folderPath, "*.dll");
            foreach (string filePath in dllFilePaths)
            {
                Assembly assembly = Assembly.LoadFrom(filePath);
                yield return assembly;
            }
        }

        private static IEnumerable<Type> EnumerateTypesInAssemblies(string folderPath)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            foreach (Assembly assembly in EnumerateAssemblies(folderPath))
            {
                if (assembly != thisAssembly) //Ignore types in this PivotServerTools assembly.
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type t in types)
                    {
                        yield return t;
                    }
                }
            }
        }
    }

}
