﻿namespace PoE.Bot.Services
{
    using Discord.WebSocket;
    using FluentScheduler;
    using Microsoft.EntityFrameworkCore;
    using PoE.Bot.Attributes;
    using PoE.Bot.Contexts;
    using System;
    using System.Linq;

    [Service]
    public class JobService
    {
        private readonly DiscordSocketClient _client;
        private readonly DatabaseContext _database;
        private readonly LeaderboardService _leaderboard;
        private readonly RssService _rss;
        private readonly StreamService _stream;

        public JobService(DiscordSocketClient client, DatabaseContext database, LeaderboardService leaderboard, RssService rss, StreamService stream)
        {
            _client = client;
            _database = database;
            _leaderboard = leaderboard;
            _rss = rss;
            _stream = stream;

            JobManager.Initialize();
        }

        public void Initialize()
        {
            JobManager.AddJob(async () =>
            {
                foreach (var mute in await _database.Users.Include(x => x.Guild).Where(x => x.Muted && x.MutedUntil < DateTime.Now).ToListAsync())
                {
                    mute.Muted = false;
                    mute.MutedUntil = default;
                    await _database.SaveChangesAsync();

                    var guild = _client.GetGuild(mute.Guild.GuildId);
                    var user = guild.GetUser(mute.UserId);
                    var role = guild.GetRole(mute.Guild.MuteRole) ?? guild.Roles.FirstOrDefault(x => x.Name is "Muted");

                    if (user is null)
                        return;

                    if (!user.Roles.Contains(role))
                        return;

                    if (user.Roles.Contains(role))
                        await user.RemoveRoleAsync(role);
                }
            }, x => x.ToRunEvery(1).Minutes());

            JobManager.AddJob(async () => await _leaderboard.ProcessLeaderboards(), x => x.ToRunEvery(30).Minutes());

            JobManager.AddJob(async () => await _stream.ProcessStreams(), x => x.ToRunEvery(5).Minutes());

            JobManager.AddJob(async () => await _rss.ProcessRssFeeds(), x => x.ToRunEvery(5).Minutes());
        }
    }
}