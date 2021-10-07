using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using DenverSpeaker.Data;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Victoria;
using Victoria.EventArgs;
using Victoria.Enums;

namespace DenverSpeaker.Services
{
    public class CommHandler
    {
        // Discord default vars.
        public static DiscordSocketClient discordClient { get; set; }
        public static CommandService discordCommands { get; set; }
        public static InteractiveService discordInteractive { get; set; }
        public static IServiceProvider discordService { get; set; }
        public static LavaNode lavaNode { get; set; }
        public static BotData botData { get; set; }
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> disconnTokens;

        public CommHandler(DiscordSocketClient _discordClient, CommandService _discordCommands, InteractiveService _discordInteractive,
            IServiceProvider _discordService, LavaNode _lavaNode, BotData _botData) {
            discordClient = _discordClient;
            discordCommands = _discordCommands;
            discordInteractive = _discordInteractive;
            discordService = _discordService;
            lavaNode = _lavaNode;
            botData = _botData;
            disconnTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            // DiscordSocketClient functions
            // When discordClient its ready connect Victoria client
            discordClient.Ready += async() => { if (!lavaNode.IsConnected) { await lavaNode.ConnectAsync(); } };
            discordClient.MessageReceived += client_NewCommandReceived;
            discordClient.UserVoiceStateUpdated += client_UserVoiceStateUpdated;
            discordClient.Log += botLogEvents;
            // LavaNode functions
            lavaNode.OnTrackStarted += lavaClient_OnTrackStarted;
            lavaNode.OnTrackEnded += lavaClient_OnTrackEnded;
            lavaNode.OnTrackStuck += lavaClient_OnTrackStuck;
            lavaNode.OnTrackException += lavaClient_OnTrackException;
        }

        private async Task client_NewCommandReceived(SocketMessage message) {
            // Block commands through DMs
            if (message.Channel is SocketDMChannel) { return; }
            // Block messages from verified bots
            if (message.Author.IsBot) { return; }
            else {
                int argPos = 0;
                SocketUserMessage discMessage = message as SocketUserMessage;
                // Detect whether the entered text will be associated with a command
                if (discMessage.HasStringPrefix(botData.BotPrefix, ref argPos)) {
                    SocketCommandContext mssgContext = new SocketCommandContext(discordClient, discMessage);
                    await discordCommands.ExecuteAsync(mssgContext, argPos, discordService);
                }
            }
        }

        private async Task client_UserVoiceStateUpdated(SocketUser argUser, SocketVoiceState argFrom, SocketVoiceState argTo) {
            try {
                // LavaNode leaves vchannel when last user leaves that specific channel
                if(lavaNode.IsConnected && !(argFrom.VoiceChannel is null)) {
                    if (!argUser.Id.Equals(discordClient.CurrentUser.Id) && argFrom.VoiceChannel.Users.Count.Equals(1))
                        await lavaNode.LeaveAsync(lavaNode.GetPlayer(argFrom.VoiceChannel.Guild).VoiceChannel);
                }
            } catch (NullReferenceException excep) {
                Console.WriteLine(excep.Message);
                await Task.CompletedTask;
            }
        }

        private async Task botLogEvents(LogMessage arg) { 
            await Task.Factory.StartNew(() => { Console.WriteLine(arg.ToString()); });
        }

        private async Task lavaClient_OnTrackStarted(TrackStartEventArgs arg) {
            await Task.Factory.StartNew(() => {
                // Cancel disconnect tasks if a new track is added/started
                if (!disconnTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value)) { return; }
                if (value.IsCancellationRequested) { return; }
                // Auto disconnect cancellation
                value.Cancel(true);
            });
        }

        private async Task lavaClient_OnTrackEnded(TrackEndedEventArgs arg) {
            // Check if the track reached the end
            if (!arg.Reason.Equals(TrackEndReason.Finished)) { return; }
            LavaPlayer player = arg.Player;
            // Queue completed
            if (!player.Queue.TryDequeue(out var queueable)) {
                // Start auto disconnect task once queue is empty
                _ = InitiateDisconnectAsync(arg.Player, TimeSpan.FromSeconds(20));
                return;
            }
            // Next item in queue is not a track
            if (!(queueable is LavaTrack track)) {
                await player.TextChannel.SendMessageAsync($"`{ queueable.Title }` is not a track. Skipping it...");
                await player.SkipAsync();
                return;
            }
            await arg.Player.PlayAsync(track);
            // Next track embed details
            EmbedBuilder embedTrack = new EmbedBuilder();
            embedTrack.Color = new Color(244, 67, 54);
            embedTrack.Title = "Playing now...";
            embedTrack.AddField("Name", track.Title, false);
            embedTrack.AddField("Author", track.Author, true);
            embedTrack.AddField("Duration", track.Duration, true);
            embedTrack.ThumbnailUrl = await track.FetchArtworkAsync();
            await arg.Player.TextChannel.SendMessageAsync(null, false, embedTrack.Build());
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan) {
            // All disconnect logic
            if (!disconnTokens.TryGetValue(player.VoiceChannel.Id, out var value)) {
                value = new CancellationTokenSource();
                disconnTokens.TryAdd(player.VoiceChannel.Id, value);
            } else if (value.IsCancellationRequested) {
                disconnTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = disconnTokens[player.VoiceChannel.Id];
            }
            // Initiate auto disconnect
            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled) { return; }
            await lavaNode.LeaveAsync(player.VoiceChannel);
        }

        private async Task lavaClient_OnTrackStuck(TrackStuckEventArgs arg) {
            try {
                // Skip current track when its stucked
                await arg.Player.SkipAsync();
            } catch (Exception excep) { Console.WriteLine(excep.Message); } 
            finally {
                // Re-added track to queue after throwing an exception
                arg.Player.Queue.Enqueue(arg.Track);
            }
        }

        private async Task lavaClient_OnTrackException(TrackExceptionEventArgs arg) {
            try {
                // Skip current track when its stucked
                await arg.Player.SkipAsync();
            } catch (Exception excep) { Console.WriteLine(excep.Message); } 
            finally {
                // Re-added track to queue after throwing an exception
                arg.Player.Queue.Enqueue(arg.Track);
            }
        }
    }
}