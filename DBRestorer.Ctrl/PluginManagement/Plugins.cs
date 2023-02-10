using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using DBRestorer.Plugin.Interface;
using Nicologies;

namespace DBRestorer.Ctrl.PluginManagement;

public static class Plugins
{
    private static List<CompositionContainer> _pluginContainers;
    private static readonly Dictionary<Type, List<object>> OutOfProcPlugins = new();

    private class OutOfProcessPluginAdapterBase 
    {
        private readonly PluginMetaData _meta;

        protected OutOfProcessPluginAdapterBase(PluginMetaData meta)
        {
            _meta = meta;
        }
        protected void Execute(string sqlInstName, string dbName, bool waitForExit)
        {
            var workingDir = Path.GetDirectoryName(_meta.Path);
            var info = new ProcessStartInfo
            {
                WorkingDirectory = workingDir ?? string.Empty,
                FileName = _meta.Path,
                Arguments = $"--sqlinstance {sqlInstName} --database {dbName}"
            };

            using (var process = Process.Start(info))
            {
                if (waitForExit)
                {
                    process?.WaitForExit();
                }
            }
        }

        public string PluginName => _meta.Name;
    }

    private class OutOfProcessPostRestorePluginAdapter : OutOfProcessPluginAdapterBase, IPostDbRestore
    {
        public OutOfProcessPostRestorePluginAdapter(PluginMetaData meta) : base(meta)
        {
        }

        public void OnDBRestored(Window parentWnd, string sqlInstName, string dbName)
        {
            Execute(sqlInstName, dbName, waitForExit: true);
        }
    }

    private class OutOfProcessUtilityPluginAdapter : OutOfProcessPluginAdapterBase, IDbUtility
    {
        public OutOfProcessUtilityPluginAdapter(PluginMetaData meta) : base(meta)
        {
        }
        public void Invoke(Window parentWnd, string sqlInstName, string dbName)
        {
            Execute(sqlInstName, dbName, waitForExit: false);
        }
    }

    private static void LoadPlugins()
    {
        if (_pluginContainers != null) return;

        _pluginContainers = new List<CompositionContainer>();
        LoadPluginsFromFolder(PathHelper.ProcessDir);

        LoadOutOfProcessPlugins();
    }

    private static void LoadOutOfProcessPlugins()
    {
        void Add(Type t, object adapter)
        {
            if (!OutOfProcPlugins.ContainsKey(t))
            {
                OutOfProcPlugins.Add(t, new List<object>
                {
                    adapter
                });
            }
            else
            {
                OutOfProcPlugins[t].Add(adapter);
            }
        }
        var plugins = OutOfProcessPluginRegistration.GetPlugins();
        foreach (var p in plugins)
        {
            switch (p.Type)
            {
                case PluginType.PostRestore:
                    Add(typeof(IPostDbRestore), new OutOfProcessPostRestorePluginAdapter(p));
                    break;
                case PluginType.Utility:
                    Add(typeof(IDbUtility), p);
                    Add(typeof(IPostDbRestore), new OutOfProcessUtilityPluginAdapter(p));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void LoadPluginsFromFolder(string pluginFolderPath)
    {
        foreach (var dll in new DirectoryInfo(pluginFolderPath).EnumerateFiles("Plugin_*.dll"))
        {
            _pluginContainers.Add(new CompositionContainer(new DirectoryCatalog(dll.DirectoryName!, dll.Name)));
        }
    }

    public static IEnumerable<Lazy<T>> GetPlugins<T>()
    {
        LoadPlugins();
        var ret = new List<Lazy<T>>();
        foreach (var container in _pluginContainers)
        {
            try
            {
                var exps = container.GetExports<T>();
                ret.AddRange(exps);
            }
            catch (ReflectionTypeLoadException ex)
            {
                Trace.TraceError("GetExports() failed {0}",
                    string.Join(Environment.NewLine, ex.LoaderExceptions.Select(r => r.ToString())));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to get exports, {ex}");
            }
        }
        ret.AddRange(OutOfProcPlugins.Where(x => x.Key == typeof(T))
            .SelectMany(x => x.Value).Select(x => new Lazy<T>(() => (T)x)));
        return ret;
    }
}