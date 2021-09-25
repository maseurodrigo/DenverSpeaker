using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace DenverSpeaker.Modules
{
    [Summary("Music Discord Commands")]
    public class MusicComms : InteractiveBase
    {
        // Getting all services through constructor param with AddSingleton()
        private readonly LavaNode lavaNode;
        private static readonly IEnumerable<int> enumRange = Enumerable.Range(1900, 2000);
        private static readonly Color embedsColor = new Color(239, 83, 80);
        private MusicComms(LavaNode _lavaNode) => this.lavaNode = _lavaNode;

        [Command("lavanode")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Get the current status of the lavanode connection")]
        public async Task getConnection() {
            // Retrieve bot connection status
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Description = $"LavaNode it's `{ new String(lavaNode.IsConnected ? "connected" : "not connected") }`";
            // Reply with the embed
            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("leave")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Disconnect the bot from the current voice channel")]
        public async Task leaveAsync() {
            EmbedBuilder userVChannel = new EmbedBuilder();
            userVChannel.Color = embedsColor;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                userVChannel.Description = $"I'm not connected to any voice channel";
                await ReplyAsync(null, false, userVChannel.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            // User it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null) {
                userVChannel.Description = $"I don't accept orders from Narnia, please join a voice channel";
                await ReplyAsync(null, false, userVChannel.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            try {
                await Context.Message.AddReactionAsync(new Emoji("👋")); // Leave emoji reaction
                await lavaNode.LeaveAsync(voiceState.VoiceChannel);
            } catch (Exception exception) { await ReplyAsync(exception.Message); }
        }

        [Command("play")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Start playing a music or playlist from the given url")]
        public async Task playURLAsync([Remainder][Summary("URL")] String _url) {
            // Remove white spaces from query
            _url = _url.Trim();
            // Parse URL params.
            NameValueCollection qString = HttpUtility.ParseQueryString(_url);
            // Checking if query string its an URL
            if (!_url.StartsWith("https") && !_url.StartsWith("http")) {
                EmbedBuilder noURLs = new EmbedBuilder();
                noURLs.Color = embedsColor;
                noURLs.Description = "For this command its needed a valid URL\nYou can use `yt` or `sc` for searching";
                await ReplyAsync(null, false, noURLs.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            } else if (!String.IsNullOrWhiteSpace(qString.Get("list"))) {
                EmbedBuilder noPlaylists = new EmbedBuilder();
                noPlaylists.Color = embedsColor;
                noPlaylists.Description = "I'm not accepting playlists at the moment";
                await ReplyAsync(null, false, noPlaylists.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            IVoiceState voiceState = Context.User as IVoiceState;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                EmbedBuilder botVChannel = new EmbedBuilder();
                botVChannel.Color = embedsColor;
                // User it's not present on any voice channel
                if (voiceState?.VoiceChannel is null) {
                    botVChannel.Description = $"I can't join you in Narnia, please join a voice channel";
                    await ReplyAsync(null, false, botVChannel.Build(), null, null, new MessageReference(Context.Message.Id));
                    return;
                }
                try { await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel); } 
                catch (Exception exception) { await ReplyAsync(exception.Message); }
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (voiceState?.VoiceChannel is null || !voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // Direct searches with a given url
            SearchResponse searchResp = await lavaNode.SearchAsync(SearchType.Direct, _url);
            // Couldnt find anything for the query param given
            if (searchResp.Status is SearchStatus.LoadFailed || searchResp.Status is SearchStatus.NoMatches) {
                EmbedBuilder noMatches = new EmbedBuilder();
                noMatches.Color = embedsColor;
                noMatches.Description = $"I didn't find anything about `{ _url }` on YouTube but I can look it up in Narnia";
                await ReplyAsync(null, false, noMatches.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            // If LavaPlayer its active (Playing/Paused)
            if (currentPlayer.PlayerState is PlayerState.Playing || currentPlayer.PlayerState is PlayerState.Paused) {
                // If its a playlist
                if (!String.IsNullOrWhiteSpace(searchResp.Playlist.Name)) {
                    foreach (var track in searchResp.Tracks) currentPlayer.Queue.Enqueue(track);
                    // Enqueued track embed
                    EmbedBuilder inQueue = new EmbedBuilder();
                    inQueue.Color = embedsColor;
                    inQueue.Description = $"Currently { searchResp.Tracks.Count } tracks in queue";
                    await ReplyAsync(null, false, inQueue.Build());
                } else {
                    LavaTrack track = searchResp.Tracks.ElementAt(0);
                    currentPlayer.Queue.Enqueue(track);
                    // Enqueued track embed
                    EmbedBuilder enQueued = new EmbedBuilder();
                    enQueued.Color = embedsColor;
                    enQueued.Description = $"Enqueued: `{ track.Title }`";
                    await ReplyAsync(null, false, enQueued.Build());
                }
            } else {
                // If its a playlist
                if (!String.IsNullOrWhiteSpace(searchResp.Playlist.Name)) {
                    foreach (var track in searchResp.Tracks) currentPlayer.Queue.Enqueue(track);
                } else {
                    // When LavaPlayer its idle trigger a PlayAsync
                    LavaTrack track = searchResp.Tracks.ElementAt(0);
                    await currentPlayer.PlayAsync(track);
                    // Next track embed details
                    EmbedBuilder embedTrack = new EmbedBuilder();
                    embedTrack.Color = embedsColor;
                    embedTrack.Title = "Playing now...";
                    embedTrack.AddField("Name", track.Title, false);
                    embedTrack.AddField("Author", track.Author, true);
                    embedTrack.AddField("Duration", track.Duration, true);
                    embedTrack.ThumbnailUrl = await track.FetchArtworkAsync();
                    await ReplyAsync(null, false, embedTrack.Build(), null, null, new MessageReference(Context.Message.Id));
                }
            }
        }

        [Command("youtube")]
        [Alias("yt")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Start playing a music from YouTube")]
        public async Task playYouTubeAsync([Remainder][Summary("YouTube query")] String _ytQuery) {
            // Remove white spaces from query
            _ytQuery = _ytQuery.Trim();
            // Checking if query string its an URL
            if (_ytQuery.StartsWith("https") || _ytQuery.StartsWith("http")) {
                EmbedBuilder noURLs = new EmbedBuilder();
                noURLs.Color = embedsColor;
                noURLs.Description = "Use the `play` command to use links";
                await ReplyAsync(null, false, noURLs.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            IVoiceState voiceState = Context.User as IVoiceState;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                EmbedBuilder botVChannel = new EmbedBuilder();
                botVChannel.Color = embedsColor;
                // User it's not present on any voice channel
                if (voiceState?.VoiceChannel is null) {
                    botVChannel.Description = $"I can't join you in Narnia, please join a voice channel";
                    await ReplyAsync(null, false, botVChannel.Build(), null, null, new MessageReference(Context.Message.Id));
                    return;
                }
                try { await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel); } 
                catch (Exception exception) { await ReplyAsync(exception.Message); }
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (voiceState?.VoiceChannel is null || !voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // YouTube searches with a given param.
            SearchResponse searchResp = await lavaNode.SearchYouTubeAsync(_ytQuery);
            // Couldnt find anything for the query param given
            if (searchResp.Status is SearchStatus.LoadFailed || searchResp.Status is SearchStatus.NoMatches) {
                EmbedBuilder noMatches = new EmbedBuilder();
                noMatches.Color = embedsColor;
                noMatches.Description = $"I didn't find anything about `{ _ytQuery }` on YouTube but I can look it up in Narnia";
                await ReplyAsync(null, false, noMatches.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            // If LavaPlayer its active (Playing/Paused)
            if (currentPlayer.PlayerState is PlayerState.Playing || currentPlayer.PlayerState is PlayerState.Paused) {
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                currentPlayer.Queue.Enqueue(track);
                // Enqueued track embed
                EmbedBuilder enQueued = new EmbedBuilder();
                enQueued.Color = embedsColor;
                enQueued.Description = $"Enqueued: `{ track.Title }`";
                await ReplyAsync(null, false, enQueued.Build());
            } else {
                // When LavaPlayer its idle trigger a PlayAsync
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                await currentPlayer.PlayAsync(track);
                // Next track embed details
                EmbedBuilder embedTrack = new EmbedBuilder();
                embedTrack.Color = embedsColor;
                embedTrack.Title = "Playing now...";
                embedTrack.AddField("Name", track.Title, false);
                embedTrack.AddField("Author", track.Author, true);
                embedTrack.AddField("Duration", track.Duration, true);
                embedTrack.ThumbnailUrl = await track.FetchArtworkAsync();
                await ReplyAsync(null, false, embedTrack.Build(), null, null, new MessageReference(Context.Message.Id));
            }
        }

        [Command("soundcloud")]
        [Alias("sc")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Start playing a music from SoundCloud")]
        public async Task playSoundCloudAsync([Remainder][Summary("SoundCloud query")] String _ytQuery) {
            // Remove white spaces from query
            _ytQuery = _ytQuery.Trim();
            // Checking if query string its an URL
            if (_ytQuery.StartsWith("https") || _ytQuery.StartsWith("http")) {
                EmbedBuilder noURLs = new EmbedBuilder();
                noURLs.Color = embedsColor;
                noURLs.Description = "Use the `play` command to use links";
                await ReplyAsync(null, false, noURLs.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            IVoiceState voiceState = Context.User as IVoiceState;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                EmbedBuilder botVChannel = new EmbedBuilder();
                botVChannel.Color = embedsColor;
                // User it's not present on any voice channel
                if (voiceState?.VoiceChannel is null) {
                    botVChannel.Description = $"I can't join you in Narnia, please join a voice channel";
                    await ReplyAsync(null, false, botVChannel.Build(), null, null, new MessageReference(Context.Message.Id));
                    return;
                }
                try { await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel); } 
                catch (Exception exception) { await ReplyAsync(exception.Message); }
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (voiceState?.VoiceChannel is null || !voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // SoundCloud searches with a given param.
            SearchResponse searchResp = await lavaNode.SearchSoundCloudAsync(_ytQuery);
            // Couldnt find anything for the query param given
            if (searchResp.Status is SearchStatus.LoadFailed || searchResp.Status is SearchStatus.NoMatches) {
                EmbedBuilder noMatches = new EmbedBuilder();
                noMatches.Color = embedsColor;
                noMatches.Description = $"I didn't find anything about `{ _ytQuery }` on SoundCloud but I can look it up in Narnia";
                await ReplyAsync(null, false, noMatches.Build(), null, null, new MessageReference(Context.Message.Id));
                return;
            }
            // If LavaPlayer its active (Playing/Paused)
            if (currentPlayer.PlayerState is PlayerState.Playing || currentPlayer.PlayerState is PlayerState.Paused) {
                // With track name -> elemAt(0)
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                currentPlayer.Queue.Enqueue(track);
                // Enqueued track embed
                EmbedBuilder enQueued = new EmbedBuilder();
                enQueued.Color = embedsColor;
                enQueued.Description = $"Enqueued: `{ track.Title }`";
                await ReplyAsync(null, false, enQueued.Build());
            } else {
                // When LavaPlayer its idle trigger a PlayAsync
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                await currentPlayer.PlayAsync(track);
                // Next track embed details
                EmbedBuilder embedTrack = new EmbedBuilder();
                embedTrack.Color = embedsColor;
                embedTrack.Title = "Playing now...";
                embedTrack.AddField("Name", track.Title, false);
                embedTrack.AddField("Author", track.Author, true);
                embedTrack.AddField("Duration", track.Duration, true);
                embedTrack.ThumbnailUrl = await track.FetchArtworkAsync();
                await ReplyAsync(null, false, embedTrack.Build(), null, null, new MessageReference(Context.Message.Id));
            }
        }

        [Command("skip")]
        [Alias("next")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Skip to next track in queue")]
        public async Task skipNextTrack() {
            // User nor bot it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null || !lavaNode.HasPlayer(Context.Guild)) { return; }
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // No next track queued
            if (currentPlayer.Queue.Count.Equals(0)) { return; }
            await currentPlayer.SkipAsync();
            if (currentPlayer.PlayerState.Equals(PlayerState.Playing)) {
                // Next track embed details
                LavaTrack currentTrack = currentPlayer.Track;
                EmbedBuilder embedTrack = new EmbedBuilder();
                embedTrack.Color = embedsColor;
                embedTrack.Title = "Playing now...";
                embedTrack.AddField("Name", currentTrack.Title, false);
                embedTrack.AddField("Author", currentTrack.Author, true);
                embedTrack.AddField("Duration", currentTrack.Duration, true);
                embedTrack.ThumbnailUrl = await currentTrack.FetchArtworkAsync();
                await Context.Message.AddReactionAsync(new Emoji("⏭️")); // Skip emoji reaction
                await ReplyAsync(null, false, embedTrack.Build(), null, null, new MessageReference(Context.Message.Id));
            }
        }

        [Command("pause")]
        [Summary("Pause current discord player")]
        public async Task pausePlayer() {
            // User nor bot it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null || !lavaNode.HasPlayer(Context.Guild)) { return; }
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // Player its already paused/stopped
            if (currentPlayer.PlayerState.Equals(PlayerState.Paused) || 
                currentPlayer.PlayerState.Equals(PlayerState.Stopped)) { return; }
            await currentPlayer.PauseAsync();
        }

        [Command("resume")]
        [Summary("Resume current discord player")]
        public async Task resumePlayer() {
            // User nor bot it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null || !lavaNode.HasPlayer(Context.Guild)) { return; }
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // Player its already paused/stopped
            if (currentPlayer.PlayerState.Equals(PlayerState.Playing)) { return; }
            await currentPlayer.ResumeAsync();
        }

        [Command("stop")]
        [Summary("Stop discord player and finish the current queue")]
        public async Task stopPlayer() {
            // User nor bot it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null || !lavaNode.HasPlayer(Context.Guild)) { return; }
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // Player its already stopped
            if (currentPlayer.PlayerState.Equals(PlayerState.Stopped)) { return; }
            await currentPlayer.StopAsync();
            currentPlayer.Queue.Clear();
        }

        [Command("queue")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("List all the tracks that are currently on queue")]
        public async Task listQueue() {
            // User nor bot it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null || !lavaNode.HasPlayer(Context.Guild)) { return; }
            // User in a different vchannel than bot
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // Queue tracks list embed
            EmbedBuilder embedQueueTracks = new EmbedBuilder();
            embedQueueTracks.Color = embedsColor;
            embedQueueTracks.Title = $"Queue: `{ currentPlayer.Queue.Count }` tracks";
            // Loop through all track in queue
            foreach (LavaTrack track in currentPlayer.Queue) 
                embedQueueTracks.AddField(track.Title, track.Author, false);
            await ReplyAsync(null, false, embedQueueTracks.Build(), null, null, new MessageReference(Context.Message.Id));
        }

        [Command("lyrics")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Searching for current track lyrics")]
        public async Task getTrackLyrics() {
            // Current track lyrics embed
            EmbedBuilder embedLyrics = new EmbedBuilder();
            embedLyrics.Color = embedsColor;
            try {
                // User nor bot it's not present on any voice channel
                IVoiceState voiceState = Context.User as IVoiceState;
                if (voiceState?.VoiceChannel is null) { return; }
                if (!lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer currentPlayer)) { return; }
                // User in a different vchannel than bot
                if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
                // Player its not playing
                if (!currentPlayer.PlayerState.Equals(PlayerState.Playing)) { return; }
                String lyrics = await currentPlayer.Track.FetchLyricsFromGeniusAsync();
                if (String.IsNullOrWhiteSpace(lyrics)) { return; }
                String[] splitLyrics = lyrics.Split('\n');
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var line in splitLyrics) {
                    if (enumRange.Contains(stringBuilder.Length)) {
                        embedLyrics.Title = currentPlayer.Track.Title;
                        embedLyrics.Description = $"```{stringBuilder}```";
                        await ReplyAsync(null, false, embedLyrics.Build(), null, null, new MessageReference(Context.Message.Id));
                        stringBuilder.Clear();
                    } else stringBuilder.AppendLine(line);
                }
                embedLyrics.Title = currentPlayer.Track.Title;
                embedLyrics.Description = $"```{stringBuilder}```";
            } catch (HttpRequestException) {
                embedLyrics.Description = "Don't ask me how, but i didn't find anything for this track";
            } catch (ArgumentOutOfRangeException) {
                embedLyrics.Description = "Sorry boss, but the lyrics for this track are a little weird and i can't present it";
            } catch (IndexOutOfRangeException excep) {
                await ReplyAsync(excep.Message);
            } finally {
                await ReplyAsync(null, false, embedLyrics.Build(), null, null, new MessageReference(Context.Message.Id));
            }
        }
    }
}