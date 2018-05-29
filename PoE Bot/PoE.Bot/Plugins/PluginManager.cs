﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PoE.Bot.Extensions;
using Discord;

namespace PoE.Bot.Plugins
{
    internal class PluginManager
    {
        public int PluginCount { get { return this.RegisteredPlugins.Count; } }
        internal IEnumerable<Assembly> PluginAssemblies { get { return this.LoadedAssemblies.Select(xkvp => xkvp.Value); } }
        internal Assembly MainAssembly { get; private set; }
        private Dictionary<string, Plugin> RegisteredPlugins { get; set; }
        private Dictionary<string, Assembly> LoadedAssemblies { get; set; }

        public PluginManager()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", "Initializing Plugin Manager"));
            this.RegisteredPlugins = new Dictionary<string, Plugin>();
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", "Plugin Manager Initialized"));
        }

        public void LoadAssemblies()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", "Loading all plugin assemblies"));
            this.LoadedAssemblies = new Dictionary<string, Assembly>();
            var a = typeof(Plugin).GetTypeInfo().Assembly;
            this.LoadedAssemblies.Add(a.GetName().Name, a);
            this.MainAssembly = a;
            var l = a.Location;
            l = Path.GetDirectoryName(l);

            var r = Path.Combine(l, "references");
            if (Directory.Exists(r))
            {
                var x = Directory.GetFiles(r, "*.dll", SearchOption.TopDirectoryOnly);
                foreach (var xx in x)
                {
                    Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", $"Loaded reference file '{xx}'"));
                    var xa = FrameworkAssemblyLoader.LoadFile(xx);
                    this.LoadedAssemblies.Add(xa.GetName().Name, xa);
                }
            }

            l = Path.Combine(l, "plugins");
            if (Directory.Exists(l))
            {
                var x = Directory.GetFiles(l, "*.dll", SearchOption.TopDirectoryOnly);
                foreach (var xx in x)
                {
                    Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", $"Loaded file '{xx}'"));
                    var xa = FrameworkAssemblyLoader.LoadFile(xx);
                    this.LoadedAssemblies.Add(xa.GetName().Name, xa);
                }
            }
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", "Registering plugin dependency resolver"));
            FrameworkAssemblyLoader.ResolvingAssembly += ResolvePlugin;
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", "Plugin dependency resolver registered"));
        }

        public void Initialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", "Registering and initializing plugins"));
            var @as = this.PluginAssemblies;
            var ts = @as.SelectMany(xa => xa.DefinedTypes);
            var pt = typeof(IPlugin);
            foreach (var t in ts)
            {
                if (!pt.IsAssignableFrom(t.AsType()) || !t.IsClass || t.IsAbstract)
                    continue;

                Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", $"Type {t.ToString()} is a plugin"));
                var iplg = Activator.CreateInstance(t.AsType()) as IPlugin;
                var plg = new Plugin { _Plugin = iplg };
                this.RegisteredPlugins.Add(plg.Name, plg);
                Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", $"Registered plugin '{plg.Name}'"));
                plg._Plugin.Initialize();
                plg._Plugin.LoadConfig(PoE_Bot.ConfigManager.GetConfig(iplg));
                Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", $"Plugin '{plg.Name}' initialized"));
            }
            this.UpdateAllConfigs();
            Log.W(new LogMessage(LogSeverity.Info, "Plugin Manager", $"Registered and initialized {this.RegisteredPlugins.Count:#,##0} plugins"));
        }

        internal void UpdateAllConfigs()
        {
            foreach (var plg in this.RegisteredPlugins)
            {
                PoE_Bot.ConfigManager.UpdateConfig(plg.Value._Plugin);
            }
        }

        private Assembly ResolvePlugin(string assembly_name)
        {
            if (this.LoadedAssemblies.ContainsKey(assembly_name))
                return this.LoadedAssemblies[assembly_name];
            return null;
        }
    }
}
