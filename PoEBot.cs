﻿namespace PoE.Bot
{
    using Addons.Interactive;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Handlers;
    using Helpers;
    using Microsoft.Extensions.DependencyInjection;
    using Objects;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class PoE_Bot
    {
        private static async Task Main(string[] args)
        {
            using (DiscordSocketClient discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 50,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Warning
            }))
            {
                IServiceCollection Services = new ServiceCollection()
                    .AddSingleton(discordSocketClient)
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        ThrowOnError = false,
                        IgnoreExtraArgs = false,
                        DefaultRunMode = RunMode.Sync,
                        CaseSensitiveCommands = false
                    }))
                    .AddSingleton(new CommandService())
                    .AddSingleton<HttpClient>()
                    .AddSingleton<DatabaseHandler>()
                    .AddSingleton<JobHandler>()
                    .AddSingleton<EventHelper>()
                    .AddSingleton<MainHandler>()
                    .AddSingleton<InteractiveService>()
                    .AddSingleton<Handlers.EventHandler>()
                    .AddSingleton(new Random(Guid.NewGuid().GetHashCode()))
                    .AddSingleton(x => x.GetRequiredService<DatabaseHandler>().Execute<ConfigObject>(Operation.Load, Id: nameof(Config)));

                ServiceProvider Provider = Services.BuildServiceProvider();
                await Provider.GetRequiredService<DatabaseHandler>().InitializeAsync();
                await Provider.GetRequiredService<MainHandler>().InitializeAsync();
                await Provider.GetRequiredService<Handlers.EventHandler>().InitializeAsync();
                Provider.GetRequiredService<JobHandler>().Initialize();
                await Task.Delay(-1);
            }
        }
    }
}