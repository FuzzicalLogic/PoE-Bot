﻿namespace PoE.Bot.Helpers
{
    using System;
    using Discord;
    using System.Linq;
    using PoE.Bot.Handlers;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    using Raven.Client.Extensions;
    using PoE.Bot.Objects;
    using System.Collections.Concurrent;
    using Drawing = System.Drawing.Color;
    using PoE.Bot.Addons;

    public class EventHelper
    {
        DatabaseHandler DB { get; }
        Random Random { get; }
        GuildHelper GuildHelper { get; }
        public TimeSpan GlobalTimeout { get; }
        ConcurrentDictionary<ulong, DateTime> WaitList { get; }
        public EventHelper(DatabaseHandler dB, Random random, GuildHelper helper)
        {
            DB = dB;
            Random = random;
            GuildHelper = helper;
            GlobalTimeout = TimeSpan.FromSeconds(30);
            WaitList = new ConcurrentDictionary<ulong, DateTime>();
        }

        internal async Task CheckStateAsync(DiscordSocketClient Client)
        {
            if (Client.ConnectionState is ConnectionState.Connected)
                return;
            var Timeout = Task.Delay(GlobalTimeout);
            var Connect = Client.StartAsync();
            var LocalTask = await Task.WhenAny(Timeout, Connect);
            if (LocalTask == Timeout || Connect.IsFaulted)
                return;
            else if (Connect.IsCompletedSuccessfully)
                return;
            else Environment.Exit(1);
        }

        internal void RunTasks(SocketUserMessage Message, GuildObject Server)
            => Task.Run(async ()
                =>
            {
                await AFKHandler(Message, Server).WithCancellation(MethodHelper.Cancellation(TimeSpan.FromSeconds(10)));
                await ModeratorAsync(Message, Server).WithCancellation(MethodHelper.Cancellation(TimeSpan.FromSeconds(10)));
            });

        Task AFKHandler(SocketMessage Message, GuildObject Server)
        {
            if (!Message.MentionedUsers.Any(x => Server.AFK.ContainsKey(x.Id)))
                return Task.CompletedTask;
            string Reason = null;
            var User = Message.MentionedUsers.FirstOrDefault(u => Server.AFK.TryGetValue(u.Id, out Reason));
            return User is null ? Task.CompletedTask : Message.Channel.SendMessageAsync($"**{User.Username} has left an AFK Message:**  {Reason}");
        }

        Task ModeratorAsync(SocketUserMessage Message, GuildObject Server)
        {
            if (GuildHelper.ProfanityMatch(Message.Content) && Server.AntiProfanity)
                return WarnUserAsync(Message, Server, $"{Message.Author.Mention}, Refrain from using profanity. You've been warned.");
            return Task.CompletedTask;
        }

        async Task WarnUserAsync(SocketUserMessage Message, GuildObject Server, string Warning)
        {
            var Guild = (Message.Author as SocketGuildUser).Guild;
            var User = Message.Author as SocketGuildUser;
            if (Server.MaxWarningsToMute is 0 || Server.MaxWarningsToPermMute is 0 || Message.Author.Id == Guild.OwnerId ||
                User.GuildPermissions.Administrator || User.GuildPermissions.ManageGuild || User.Roles.Where(r => r.Name is "Moderator").Any())
                return;
            await Message.DeleteAsync();
            var Profile = GuildHelper.GetProfile(DB, Guild.Id, Message.Author.Id);
            Profile.Warnings++;
            if (Profile.Warnings >= Server.MaxWarningsToPermMute)
            {
                DateTime Now = DateTime.Now;
                TimeSpan Span = Now.AddYears(999) - Now;
                await MuteCommandAsync(Message, Server, User, Span, "Muted permanently by AutoMod.");
                await GuildHelper.LogAsync(DB, Guild, Message.Author, Guild.CurrentUser, CaseType.AUTOMODPERMMUTE, Warning);
            }
            else if (Profile.Warnings >= Server.MaxWarningsToMute)
            {
                await MuteCommandAsync(Message, Server, User, TimeSpan.FromDays(1), "Muted for 1 day by AutoMod.");
                await GuildHelper.LogAsync(DB, Guild, Message.Author, Guild.CurrentUser, CaseType.AUTOMODMUTE, Warning);
            }
            else
            {
                GuildHelper.SaveProfile(DB, Guild.Id, Message.Author.Id, Profile);
                await GuildHelper.LogAsync(DB, Guild, Message.Author, Guild.CurrentUser, CaseType.WARNING, Warning);
            }
            await Message.Channel.SendMessageAsync(Warning);
        }

        async Task MuteCommandAsync(SocketUserMessage Message, GuildObject Server, IGuildUser User, TimeSpan Time, string Reason)
        {
            var Guild = (Message.Author as SocketGuildUser).Guild;
            await User.AddRoleAsync(Guild.GetRole(Server.MuteRole));

            var Embed = Extras.Embed(Drawing.Aqua)
                .WithAuthor(Guild.CurrentUser)
                .WithTitle("Mod Action")
                .WithDescription($"You were muted in the {Guild.Name} server.")
                .WithThumbnailUrl(Guild.CurrentUser.GetAvatarUrl())
                .WithFooter($"You can PM a Mod directly to resolve the issue.")
                .AddField("Reason", Reason)
                .AddField("Duration", StringHelper.FormatTimeSpan(Time))
                .Build();

            await (await User.GetOrCreateDMChannelAsync()).SendMessageAsync(embed: Embed);
            Server.Muted.TryAdd(User.Id, DateTime.Now.Add(Time));
        }
    }
}
