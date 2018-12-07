﻿namespace PoE.Bot.Parsers
{
    using Discord;
    using PoE.Bot.Attributes;
    using Qmmands;
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [ConcreteType(typeof(IEmote))]
    public class DiscordEmoteTypeParser : TypeParser<IEmote>
    {
        public override Task<TypeParserResult<IEmote>> ParseAsync(string value, ICommandContext _, IServiceProvider __)
        {
            if (Emote.TryParse(value, out Emote emote))
            {
                return Task.FromResult(new TypeParserResult<IEmote>(emote));
            }
            else if (Regex.Match(value, @"[^\s\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9-\u21AA\u231A-\u231B\u2328\u23CF\u23E9-\u23F3\u23F8-\u23FA\u24C2\u25AA-\u25AB\u25B6\u25C0\u25FB-\u25FE\u2600-\u2604\u260E\u2611\u2614-\u2615\u2618\u261D\u2620\u2622-\u2623\u2626\u262A\u262E-\u262F\u2638-\u263A\u2648-\u2653\u2660\u2663\u2665-\u2666\u2668\u267B\u267F\u2692-\u2694\u2696-\u2697\u2699\u269B-\u269C\u26A0-\u26A1\u26AA-\u26AB\u26B0-\u26B1\u26BD-\u26BE\u26C4-\u26C5\u26C8\u26CE-\u26CF\u26D1\u26D3-\u26D4\u26E9-\u26EA\u26F0-\u26F5\u26F7-\u26FA\u26FD\u2702\u2705\u2708-\u270D\u270F\u2712\u2714\u2716\u271D\u2721\u2728\u2733-\u2734\u2744\u2747\u274C\u274E\u2753-\u2755\u2757\u2763-\u2764\u2795-\u2797\u27A1\u27B0\u27BF\u2934-\u2935\u2B05-\u2B07\u2B1B-\u2B1C\u2B50\u2B55\u3030\u303D\u3297\u3299\u1F004\u1F0CF\u1F170-\u1F171\u1F17E-\u1F17F\u1F18E\u1F191-\u1F19A\u1F201-\u1F202\u1F21A\u1F22F\u1F232-\u1F23A\u1F250-\u1F251\u1F300-\u1F321\u1F324-\u1F393\u1F396-\u1F397\u1F399-\u1F39B\u1F39E-\u1F3F0\u1F3F3-\u1F3F5\u1F3F7-\u1F4FD\u1F4FF-\u1F53D\u1F549-\u1F54E\u1F550-\u1F567\u1F56F-\u1F570\u1F573-\u1F579\u1F587\u1F58A-\u1F58D\u1F590\u1F595-\u1F596\u1F5A5\u1F5A8\u1F5B1-\u1F5B2\u1F5BC\u1F5C2-\u1F5C4\u1F5D1-\u1F5D3\u1F5DC-\u1F5DE\u1F5E1\u1F5E3\u1F5EF\u1F5F3\u1F5FA-\u1F64F\u1F680-\u1F6C5\u1F6CB-\u1F6D0\u1F6E0-\u1F6E5\u1F6E9\u1F6EB-\u1F6EC\u1F6F0\u1F6F3\u1F910-\u1F918\u1F980-\u1F984\u1F9C0]+", RegexOptions.IgnoreCase).Success)
            {
                return Task.FromResult(new TypeParserResult<IEmote>(new Emoji(value)));
            }
            else
            {
                return Task.FromResult(new TypeParserResult<IEmote>("Emote not found."));
            }
        }
    }
}