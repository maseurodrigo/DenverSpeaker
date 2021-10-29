using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DenverSpeaker.Modules
{
    [Summary("General Discord Commands")]
    public class GeneralComms : ModuleBase<SocketCommandContext>
    {
        // Getting all services through constructor param with AddSingleton()
        private readonly CommandService commandService;
        private GeneralComms(CommandService _commandService) => commandService = _commandService;

        [Command("help")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task getHelp() {
            // List of all available commands
            List<CommandInfo> allCommands = commandService.Commands.OrderBy(comm => comm.Name).ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();
            // List of commands to exclude from help list
            Dictionary<String, bool> commsToExclude = new Dictionary<String, bool>() { { "conn", false }, { "help", false }, { "lavanode", false } };
            foreach (CommandInfo command in allCommands) {
                if (!commsToExclude.ContainsKey(command.Name)) {
                    // Get the command Summary attribute information
                    String embedFieldText = command.Summary ?? "No description available";
                    embedBuilder.AddField($"`{ command.Name.ToLower() }`", embedFieldText);
                }
            }
            // Reply with the embed
            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("conn")]
        [Alias("connection", "status")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [Summary("Get the current status of the bot connection")]
        public async Task getConnection() {
            // Retrieve bot connection status
            EmbedBuilder embedBuilder = new EmbedBuilder();
            ConnectionState currentConn = Context.Client.ConnectionState;
            embedBuilder.AddField("Login", Context.Client.LoginState, true);
            embedBuilder.AddField("Connection", currentConn, true);
            if (currentConn.Equals(ConnectionState.Connected))
                embedBuilder.AddField("Latency", $"{ Context.Client.Latency } ms", true);
            // Reply with the embed
            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("ytparty")]
        [Summary("Watch **YouTube** videos with other users on a voice channel")]
        public async Task discTogether_YTParty() { await Task.CompletedTask; }
    }
}