using System;
using Newtonsoft.Json;

namespace DenverSpeaker.Data
{
    public class BotData {
        [JsonProperty("DiscordBotPrefix")] public String BotPrefix { get; set; }
        [JsonProperty("DiscordBotToken")] public String BotToken { get; set; }
    }
}