// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using TrakHound.Api.v2;

namespace TrakHound.TempServer
{
    static class Modules
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private static List<IRestModule> _modules = new List<IRestModule>();
        public static ReadOnlyCollection<IRestModule> LoadedModules
        {
            get { return _modules.AsReadOnly(); }
        }

        public static void Load()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);

            // Get Modules embedded in the current assembly
            var modules = FindModules(assemblyDir);
            if (modules != null)
            {
                foreach (var module in modules) log.Info("Rest Module Loaded : " + module.Name);
            }

            _modules.AddRange(modules);
        }

        public static List<IRestModule> Get()
        {
            var l = new List<IRestModule>();

            foreach (var module in _modules)
            {
                l.Add((IRestModule)Activator.CreateInstance(module.GetType()));
            }

            return l;
        }

        public static IRestModule Get(Type t)
        {
            return (IRestModule)Activator.CreateInstance(t);
        }


        private class ModuleContainer
        {
            [ImportMany(typeof(IRestModule))]
            public IEnumerable<Lazy<IRestModule>> Modules { get; set; }
        }

        private static List<IRestModule> FindModules(string dir)
        {
            if (dir != null)
            {
                if (Directory.Exists(dir))
                {
                    var catalog = new DirectoryCatalog(dir);
                    var container = new CompositionContainer(catalog);
                    return FindModules(container);
                }
            }

            return null;
        }

        private static List<IRestModule> FindModules(Assembly assembly)
        {
            if (assembly != null)
            {
                var catalog = new AssemblyCatalog(assembly);
                var container = new CompositionContainer(catalog);
                return FindModules(container);
            }

            return null;
        }

        private static List<IRestModule> FindModules(CompositionContainer container)
        {
            try
            {
                var moduleContainer = new ModuleContainer();
                container.SatisfyImportsOnce(moduleContainer);

                if (moduleContainer.Modules != null)
                {
                    var modules = new List<IRestModule>();

                    foreach (var lModule in moduleContainer.Modules)
                    {
                        try
                        {
                            var module = lModule.Value;
                            modules.Add(module);
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex, "Rest Module Initialization Error");
                        }
                    }

                    return modules;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                log.Error(ex);

                foreach (var lex in ex.LoaderExceptions)
                {
                    log.Error(lex);
                }
            }
            catch (UnauthorizedAccessException ex) { log.Error(ex); }
            catch (Exception ex) { log.Error(ex); }

            return null;
        }
    }
}
