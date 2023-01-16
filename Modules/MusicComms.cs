using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Discord.Commands;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

using SummaryAttribute = Discord.Commands.SummaryAttribute;
using RequireUserPermissionAttribute = Discord.Commands.RequireUserPermissionAttribute;
using RequireBotPermissionAttribute = Discord.Commands.RequireBotPermissionAttribute;

namespace DenverSpeaker.Modules
{
    [Summary("Music Discord Commands")]
    public class MusicComms : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
    {
        // Getting all services through constructor param with AddSingleton()
        private readonly LavaNode lavaNode;
        private static readonly Color embedsColor = new Color(239, 83, 80);
        private MusicComms(LavaNode _lavaNode) => lavaNode = _lavaNode;

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
                await ReplyAsync(null, false, userVChannel.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            // User it's not present on any voice channel
            IVoiceState voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel is null) {
                userVChannel.Description = $"I don't accept orders from Narnia, please join a voice channel";
                await ReplyAsync(null, false, userVChannel.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            try {
                await Context.Interaction.Message.AddReactionAsync(new Emoji("👋")); // Leave emoji reaction
                await lavaNode.LeaveAsync(voiceState.VoiceChannel);
            } catch (Exception exception) { await ReplyAsync(exception.Message); }
        }

        [Command("play")]
        [Alias("p")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Start playing a music or playlist from the given url")]
        public async Task playURLAsync([Remainder][Summary("URL")] String _url) {
            EmbedBuilder playURLEmbed = new EmbedBuilder();
            playURLEmbed.Color = embedsColor;
            // Remove white spaces from query
            _url = _url.Trim();
            // Parse URL params.
            NameValueCollection qString = HttpUtility.ParseQueryString(_url);
            // Checking if query string its an URL
            if (!Uri.IsWellFormedUriString(_url, UriKind.Absolute)) {
                playURLEmbed.Description = "For this command its needed a valid URL!\nYou can use `yt` or `sc` for searching";
                await ReplyAsync(null, false, playURLEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            } else if (!String.IsNullOrWhiteSpace(qString.Get("list"))) {
                if(qString.Count > 1) {
                    playURLEmbed.Description = "This link doesnt correspond to a valid playlist";
                    await ReplyAsync(null, false, playURLEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                    return;
                }
            }
            IVoiceState voiceState = Context.User as IVoiceState;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                // User it's not present on any voice channel
                if (voiceState?.VoiceChannel is null) {
                    playURLEmbed.Description = $"I can't join you in Narnia, please join a voice channel";
                    await ReplyAsync(null, false, playURLEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                    return;
                }
                try { await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel); } 
                catch (Exception exception) { await ReplyAsync(exception.Message); }
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // Direct searches with a given url
            SearchResponse searchResp = await lavaNode.SearchAsync(SearchType.Direct, _url);
            // Couldnt find anything for the query param given
            if (searchResp.Status is SearchStatus.LoadFailed || searchResp.Status is SearchStatus.NoMatches) {
                playURLEmbed.Description = $"I didn't find anything for `{ _url }`";
                await ReplyAsync(null, false, playURLEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            // If LavaPlayer its active (Playing/Paused)
            if (currentPlayer.PlayerState is PlayerState.Playing || currentPlayer.PlayerState is PlayerState.Paused) {
                // If its a playlist
                if (!String.IsNullOrWhiteSpace(searchResp.Playlist.Name)) {
                    foreach (var track in searchResp.Tracks) { currentPlayer.Queue.Enqueue(track); }
                    // Enqueued track embed
                    playURLEmbed.Description = $"Currently `{ searchResp.Tracks.Count }` tracks in queue";
                    await ReplyAsync(null, false, playURLEmbed.Build());
                } else {
                    LavaTrack track = searchResp.Tracks.ElementAt(0);
                    currentPlayer.Queue.Enqueue(track);
                    // Enqueued track embed
                    playURLEmbed.Description = $"Enqueued: `{ track.Title }`";
                    await ReplyAsync(null, false, playURLEmbed.Build());
                }
            } else {
                // If its a playlist
                if (!String.IsNullOrWhiteSpace(searchResp.Playlist.Name)) {
                    foreach (LavaTrack track in searchResp.Tracks.Skip(1)) { currentPlayer.Queue.Enqueue(track); }
                    // Enqueued track embed
                    playURLEmbed.Description = $"Currently `{ searchResp.Tracks.Count }` tracks in queue";
                    await ReplyAsync(null, false, playURLEmbed.Build());
                }
                // Get first enqueued track
                LavaTrack startTrack = searchResp.Tracks.ElementAt(0);
                await currentPlayer.PlayAsync(startTrack);
                // Next track embed details
                playURLEmbed.Title = "Playing now...";
                playURLEmbed.AddField("Name", startTrack.Title, false);
                playURLEmbed.AddField("Author", startTrack.Author, true);
                playURLEmbed.AddField("Duration", startTrack.Duration, true);
                playURLEmbed.ThumbnailUrl = await startTrack.FetchArtworkAsync();
                await ReplyAsync(null, false, playURLEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
            }
        }

        [Command("youtube")]
        [Alias("yt")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Start playing a music from YouTube")]
        public async Task playYouTubeAsync([Remainder][Summary("YouTube query")] String _ytQuery) {
            EmbedBuilder playYTEmbed = new EmbedBuilder();
            playYTEmbed.Color = embedsColor;
            // Remove white spaces from query
            _ytQuery = _ytQuery.Trim();
            // Checking if query string its an URL
            if (Uri.IsWellFormedUriString(_ytQuery, UriKind.Absolute)) {
                playYTEmbed.Description = "Use the `play` command to use links";
                await ReplyAsync(null, false, playYTEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            IVoiceState voiceState = Context.User as IVoiceState;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                // User it's not present on any voice channel
                if (voiceState?.VoiceChannel is null) {
                    playYTEmbed.Description = $"I can't join you in Narnia, please join a voice channel";
                    await ReplyAsync(null, false, playYTEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                    return;
                }
                try { await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel); } 
                catch (Exception exception) { await ReplyAsync(exception.Message); }
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // YouTube searches with a given param.
            SearchResponse searchResp = await lavaNode.SearchYouTubeAsync(_ytQuery);
            // Couldnt find anything for the query param given
            if (searchResp.Status is SearchStatus.LoadFailed || searchResp.Status is SearchStatus.NoMatches) {
                playYTEmbed.Description = $"I didn't find anything about `{ _ytQuery }` on YouTube, but I can look it up in Narnia";
                await ReplyAsync(null, false, playYTEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            // If LavaPlayer its active (Playing/Paused)
            if (currentPlayer.PlayerState is PlayerState.Playing || currentPlayer.PlayerState is PlayerState.Paused) {
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                currentPlayer.Queue.Enqueue(track);
                // Enqueued track embed
                playYTEmbed.Description = $"Enqueued: `{ track.Title }`";
                await ReplyAsync(null, false, playYTEmbed.Build());
            } else {
                // When LavaPlayer its idle trigger a PlayAsync
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                await currentPlayer.PlayAsync(track);
                // Next track embed details
                playYTEmbed.Title = "Playing now...";
                playYTEmbed.AddField("Name", track.Title, false);
                playYTEmbed.AddField("Author", track.Author, true);
                playYTEmbed.AddField("Duration", track.Duration, true);
                playYTEmbed.ThumbnailUrl = await track.FetchArtworkAsync();
                await ReplyAsync(null, false, playYTEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
            }
        }

        [Command("soundcloud")]
        [Alias("sc")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Start playing a music from SoundCloud")]
        public async Task playSoundCloudAsync([Remainder][Summary("SoundCloud query")] String _ytQuery) {
            EmbedBuilder playSCEmbed = new EmbedBuilder();
            playSCEmbed.Color = embedsColor;
            // Remove white spaces from query
            _ytQuery = _ytQuery.Trim();
            // Checking if query string its an URL
            if (Uri.IsWellFormedUriString(_ytQuery, UriKind.Absolute)) {
                playSCEmbed.Description = "Use the `play` command to use links";
                await ReplyAsync(null, false, playSCEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            IVoiceState voiceState = Context.User as IVoiceState;
            // Lava client it's not present on any voice channel
            if (!lavaNode.HasPlayer(Context.Guild)) {
                // User it's not present on any voice channel
                if (voiceState?.VoiceChannel is null) {
                    playSCEmbed.Description = $"I can't join you in Narnia, please join a voice channel";
                    await ReplyAsync(null, false, playSCEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                    return;
                }
                try { await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel); } 
                catch (Exception exception) { await ReplyAsync(exception.Message); }
            }
            // Assign LavaPlayer to the current discord server
            LavaPlayer currentPlayer = lavaNode.GetPlayer(Context.Guild);
            // User in a different vchannel than bot
            if (!voiceState.VoiceChannel.Equals(currentPlayer.VoiceChannel)) { return; }
            // SoundCloud searches with a given param.
            SearchResponse searchResp = await lavaNode.SearchSoundCloudAsync(_ytQuery);
            // Couldnt find anything for the query param given
            if (searchResp.Status is SearchStatus.LoadFailed || searchResp.Status is SearchStatus.NoMatches) {
                playSCEmbed.Description = $"I didn't find anything about `{ _ytQuery }` on SoundCloud, but I can look it up in Narnia";
                await ReplyAsync(null, false, playSCEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
                return;
            }
            // If LavaPlayer its active (Playing/Paused)
            if (currentPlayer.PlayerState is PlayerState.Playing || currentPlayer.PlayerState is PlayerState.Paused) {
                // With track name -> elemAt(0)
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                currentPlayer.Queue.Enqueue(track);
                // Enqueued track embed
                playSCEmbed.Description = $"Enqueued: `{ track.Title }`";
                await ReplyAsync(null, false, playSCEmbed.Build());
            } else {
                // When LavaPlayer its idle trigger a PlayAsync
                LavaTrack track = searchResp.Tracks.ElementAt(0);
                await currentPlayer.PlayAsync(track);
                // Next track embed details
                playSCEmbed.Title = "Playing now...";
                playSCEmbed.AddField("Name", track.Title, false);
                playSCEmbed.AddField("Author", track.Author, true);
                playSCEmbed.AddField("Duration", track.Duration, true);
                playSCEmbed.ThumbnailUrl = await track.FetchArtworkAsync();
                await ReplyAsync(null, false, playSCEmbed.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
            }
        }

        [Command("skip")]
        [Alias("next")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Skip to the next track in queue")]
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
                await Context.Interaction.Message.AddReactionAsync(new Emoji("⏭️")); // Skip emoji reaction
                await ReplyAsync(null, false, embedTrack.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
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
            await Context.Interaction.Message.AddReactionAsync(new Emoji("⏸️")); // Pause emoji reaction
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
            await Context.Interaction.Message.AddReactionAsync(new Emoji("⏯️")); // Resume emoji reaction
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
            await Context.Interaction.Message.AddReactionAsync(new Emoji("⏹️")); // Stop emoji reaction
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
            if (currentPlayer.Queue.Count.Equals(0)) { embedQueueTracks.Description = "Currently, theres no tracks in queue"; } 
            else {
                embedQueueTracks.Title = $"Queue: `{ currentPlayer.Queue.Count }` tracks";
                // Loop through all track in queue
                foreach (LavaTrack track in currentPlayer.Queue) embedQueueTracks.AddField(track.Title, track.Author, false);
            }
            await ReplyAsync(null, false, embedQueueTracks.Build(), null, null, new MessageReference(Context.Interaction.Message.Id));
        }
    }
}