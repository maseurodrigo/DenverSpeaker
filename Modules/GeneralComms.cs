using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DenverSpeaker.Modules
{
    [Summary("General Discord Commands")]
    public class GeneralComms : ModuleBase<SocketCommandContext>
    {
        // Getting all services through constructor param with AddSingleton()
        private readonly CommandService commandService;
        private GeneralComms(CommandService _commandService) => this.commandService = _commandService;

        [Command("help")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task getHelp() {
            // List of all available commands
            List<CommandInfo> allCommands = commandService.Commands.ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();
            EmbedBuilder embedAdminBuilder = new EmbedBuilder();
            // Instance context user data
            SocketGuildUser contextUser = Context.User as SocketGuildUser;
            GuildPermissions userGuildPerms = contextUser.GuildPermissions;
            // List of commands to exclude from help list
            Dictionary<String, bool> commsToExclude = new Dictionary<String, bool>() { { "help", false }, { "lavanode", false } };
            foreach (CommandInfo command in allCommands) {
                if (commsToExclude.ContainsKey(command.Name)) {
                    if (commsToExclude[command.Name]) {
                        // Get the command Summary attribute information
                        String embedFieldText = command.Summary ?? "No description available\n";
                        embedAdminBuilder.AddField(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.Name.ToLower()), embedFieldText);
                    }
                } else {
                    // Get the command Summary attribute information
                    String embedFieldText = command.Summary ?? "No description available\n";
                    embedBuilder.AddField(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.Name.ToLower()), embedFieldText);
                }
            }
            // Reply with the embed
            await ReplyAsync(null, false, embedBuilder.Build());
            // DM reply with the admin's embed
            if (userGuildPerms.Administrator) {
                embedAdminBuilder.Title = "Exclusive admin commands";
                await contextUser.SendMessageAsync(null, false, embedAdminBuilder.Build());
            }
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
    }
}