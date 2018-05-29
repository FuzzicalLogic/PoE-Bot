﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Reflection;
using Discord;
using PoE.Bot.Config;
using PoE.Bot.Plugins;
using Newtonsoft.Json.Linq;
using TwitchLib.Api;

namespace PoE.Bot.Plugin.Twitch
{
    public class TwitchPlugin : IPlugin
    {
        private static TwitchAPI twitchAPI;
        public IPluginConfig Config { get { return this.conf; } }
        public Type ConfigType { get { return typeof(TwitchPluginConfig); } }
        public string Name { get { return "Twitch Plugin"; } }
        private Timer TwitchTimer { get; set; }
        private TwitchPluginConfig conf;

        public static TwitchPlugin Instance { get; private set; }

        public void Initialize()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Twitch Plugin", "Initializing Twitch"));
            Instance = this;
            this.conf = new TwitchPluginConfig();

            var a = typeof(PoE.Bot.Core.Client).GetTypeInfo().Assembly;
            var l = Path.GetDirectoryName(a.Location);
            var sp = Path.Combine(l, "config.json");
            var sjson = File.ReadAllText(sp, Encoding.UTF8);
            var sjo = JObject.Parse(sjson);
            var clientID = (string)sjo["twitchClientID"];
            var accessToken = (string)sjo["twitchAccessToken"];

            twitchAPI = new TwitchAPI();
            twitchAPI.Settings.ClientId = clientID;
            twitchAPI.Settings.AccessToken = accessToken;

            this.TwitchTimer = new Timer(new TimerCallback(Twitch_Tick), null, 5000, 900000);
            Log.W(new LogMessage(LogSeverity.Info, "Twitch Plugin", "Twitch Initialized"));
        }

        public void LoadConfig(IPluginConfig config)
        {
            var cfg = config as TwitchPluginConfig;
            if (cfg != null)
                this.conf = cfg;
        }

        public void AddStream(string name, string userid, ulong channel)
        {
            this.conf.Streams.Add(new Twitch(name, userid, false, channel));
            Log.W(new LogMessage(LogSeverity.Info, "Twitch Plugin", $"Added Twitch stream for {name}: {channel}"));

            UpdateConfig();
        }

        public void RemoveStream(string name, ulong channel)
        {
            var feed = this.conf.Streams.FirstOrDefault(xf => xf.Name == name && xf.ChannelId == channel);
            this.conf.Streams.Remove(feed);
            Log.W(new LogMessage(LogSeverity.Info, "Twitch Plugin", $"Removed Twitch stream for {name}: {channel}"));

            UpdateConfig();
        }

        internal IEnumerable<Twitch> GetStreams(ulong[] channels)
        {
            foreach (var stream in this.conf.Streams)
                if (channels.Contains(stream.ChannelId))
                    yield return stream;
        }

        private void UpdateConfig()
        {
            Log.W(new LogMessage(LogSeverity.Info, "Twitch Plugin", "Updating config"));
            PoE_Bot.ConfigManager.UpdateConfig(this);
        }

        private async void Twitch_Tick(object _)
        {
            foreach(var stream in this.conf.Streams)
            {
                var a = typeof(PoE.Bot.Core.Client).GetTypeInfo().Assembly;
                var l = Path.GetDirectoryName(a.Location);
                var sp = Path.Combine(l, "config.json");
                var sjson = File.ReadAllText(sp, Encoding.UTF8);
                var sjo = JObject.Parse(sjson);
                var clientID = (string)sjo["twitchClientID"];
                var accessToken = (string)sjo["twitchAccessToken"];
                var streamWasLive = stream.IsLive;

                twitchAPI = new TwitchAPI();
                twitchAPI.Settings.ClientId = clientID;
                twitchAPI.Settings.AccessToken = accessToken;

                var streamData = await twitchAPI.Streams.helix.GetStreamsAsync(null, null, 100, null, null, "live", new List<string>(new string[] { stream.UserId }), null);
                if (streamData.Streams.Count() == 0)
                    stream.IsLive = false;
                
                foreach (var s in streamData.Streams)
                {
                    if (!stream.IsLive)
                    {
                        var twitchUser = await twitchAPI.Users.helix.GetUsersAsync(new List<string>(new string[] { s.UserId }));
                        var twitchGame = await twitchAPI.Games.helix.GetGamesAsync(new List<string>(new string[] { s.GameId }));
                        var embed = new EmbedBuilder();
                        embed.WithTitle(s.Title)
                            .WithDescription($"\n**{GetTwitchDisplayName(twitchUser.Users)}** is playing **{GetTwitchGameName(twitchGame.Games)}** for {s.ViewerCount} viewers!\n\n**http://www.twitch.tv/{GetTwitchDisplayName(twitchUser.Users)}**")
                            .WithAuthor(GetTwitchDisplayName(twitchUser.Users), GetTwitchProfileImage(twitchUser.Users), $"http://www.twitch.tv/{GetTwitchDisplayName(twitchUser.Users)}")
                            .WithThumbnailUrl(GetTwitchGameBoxArt(twitchGame.Games))
                            .WithImageUrl(s.ThumbnailUrl.Replace("{width}x{height}", "640x360"))
                            .WithColor(new Color(0, 127, 255));

                        PoE_Bot.Client.SendEmbed(embed, stream.ChannelId);

                        stream.IsLive = true;
                        UpdateConfig();
                        await Task.Delay(15000);
                    }
                }
                if (streamWasLive && stream.IsLive == false)
                    UpdateConfig();
            }

            Log.W(new LogMessage(LogSeverity.Info, "Twitch Plugin", "Ticked Twitch"));
        }

        public string GetTwitchDisplayName(TwitchLib.Api.Models.Helix.Users.GetUsers.User[] user)
        {
            if (user.Count() == 0)
                return "Invalid_User";

            return user[0].DisplayName;
        }

        public string GetTwitchProfileImage(TwitchLib.Api.Models.Helix.Users.GetUsers.User[] user)
        {
            if (user.Count() == 0)
                return "https://static-cdn.jtvnw.net/user-default-pictures/0ecbb6c3-fecb-4016-8115-aa467b7c36ed-profile_image-600x600.jpg";

            return user[0].ProfileImageUrl;
        }

        public string GetTwitchGameName(TwitchLib.Api.Models.Helix.Games.GetGames.Game[] game)
        {
            if (game.Count() == 0)
                return "Invalid_Game";

            return game[0].Name;
        }

        public string GetTwitchGameBoxArt(TwitchLib.Api.Models.Helix.Games.GetGames.Game[] game)
        {
            if (game.Count() == 0)
                return "https://static-cdn.jtvnw.net/ttv-static/404_boxart-285x380.jpg";

            return game[0].BoxArtUrl.Replace("{width}x{height}", "285x380");
        }
    }
}
