const Discord = require('discord.js');
const { MessageEmbed } = require('discord.js');
const client = new Discord.Client({ intents: [Discord.Intents.FLAGS.GUILDS, Discord.Intents.FLAGS.GUILD_MESSAGES] });
const { DiscordTogether } = require('./index.js');
var BotData = require('./BotData.json');

client.discordTogether = new DiscordTogether(client);
const notConnected = new MessageEmbed().setColor('#EF5350')
    .setDescription("I can't invite you to Narnia, please join a voice channel");

// 'message' for Discord.js v12
client.on('messageCreate', async message => {
    if (message.content === BotData.DiscordBotPrefix.concat('ytparty')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            console.log(`${message.member.voice.channel.id}: ${message.content}`);
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'youtube').then(async invite => {
                return message.channel.send(`${invite.code}`);
            });
        } else { message.channel.send({ embeds: [notConnected] }); };
    } else if (message.content === BotData.DiscordBotPrefix.concat('poker')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            console.log(`${message.member.voice.channel.id}: ${message.content}`);
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'poker').then(async invite => {
                return message.channel.send(`${invite.code}`);
            });
        } else { message.channel.send({ embeds: [notConnected] }); };
    } else if (message.content === BotData.DiscordBotPrefix.concat('chess')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            console.log(`${message.member.voice.channel.id}: ${message.content}`);
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'chess').then(async invite => {
                return message.channel.send(`${invite.code}`);
            });
        } else { message.channel.send({ embeds: [notConnected] }); };
    };
});

// Login with bot token
client.login(BotData.DiscordBotToken);