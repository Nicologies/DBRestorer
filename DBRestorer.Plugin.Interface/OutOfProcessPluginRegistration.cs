using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace DBRestorer.Plugin.Interface
{
    public enum PluginType
    {
        PostRestore,
        Utility
    }

    public class PluginMetaData
    {
        public PluginMetaData(string name, string path, PluginType type = PluginType.PostRestore)
        {
            Path = path;
            Name = name;
            Type = type;
        }

        public PluginType Type { get; set; }
        public string Name { get; private set; }
        public string Path { get; private set; }
    }
    public class OutOfProcessPluginRegistration
    {
        private const string CompanyName = "Nicologies";
        private static readonly string PluginsPath = $@"Software\{CompanyName}\Plugins";

        public void Register(PluginMetaData pluginMeta)
        {
            var path = GetKeyPath(pluginMeta.Name);
            var key = Registry.CurrentUser.OpenSubKey(path, writable: true) 
                      ?? Registry.CurrentUser.CreateSubKey(path, writable: true);

            if (key == null)
            {
                throw new NullReferenceException($"Unable to create key {path}");
            }

            using (key)
            {
                key.SetValue(nameof(PluginMetaData.Type), pluginMeta.Type);
                key.SetValue(nameof(PluginMetaData.Path), pluginMeta.Path);
            }
        }

        private static string GetKeyPath(string name)
        {
            return $@"{PluginsPath}\{name}";
        }


        public void RemovePlugin(string name)
        {
            var path = GetKeyPath(name);
            var key = Registry.CurrentUser.OpenSubKey(path);
            if (key == null) return;
            key.Close();
            Registry.CurrentUser.DeleteSubKey(path);
        }

        public static IEnumerable<PluginMetaData> GetPlugins()
        {
            var key = Registry.CurrentUser.OpenSubKey(PluginsPath);
            if (key == null)
            {
                yield break;
            }

            using (key)
            {
                var plugins = key.GetSubKeyNames();

                foreach (var name in plugins)
                {
                    using (var pluginKey = key.OpenSubKey(name))
                    {
                        if (pluginKey == null)
                        {
                            continue;
                        }

                        var parsed =
                            Enum.TryParse<PluginType>(pluginKey.GetValue(nameof(PluginMetaData.Type)).ToString(),
                                out var type);
                        if (!parsed)
                        {
                            continue;
                        }

                        yield return new PluginMetaData(
                            name,
                            pluginKey.GetValue(nameof(PluginMetaData.Path)).ToString(),
                            type);
                    }
                }
            }
        }
    }
}
