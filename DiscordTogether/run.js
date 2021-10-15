const { Client, MessageEmbed } = require('discord.js');
// Create a new client instance
const client = new Client({ intents: 641 });
const { DiscordTogether } = require('./index.js');
client.discordTogether = new DiscordTogether(client);

const notConnected = new MessageEmbed().setColor('#EF5350').setDescription("I can't invite you to Narnia, please join a voice channel");
var BotData = require('./BotData.json');

client.on('messageCreate', async message => {
    if (message.content === BotData.DiscordBotPrefix.concat('ytparty')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'youtube').then(async invite => {
                console.log(`${message.member.voice.channel.id}: ${message.content}`);
                return message.reply(`${invite.code}`).then(msg => { setTimeout(() => msg.delete(), 10000) });
            });
        } else { message.reply({ embeds: [notConnected] }); };
    } else if (message.content === BotData.DiscordBotPrefix.concat('poker')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'poker').then(async invite => {
                console.log(`${message.member.voice.channel.id}: ${message.content}`);
                return message.reply(`${invite.code}`).then(msg => { setTimeout(() => msg.delete(), 10000) });
            });
        } else { message.reply({ embeds: [notConnected] }); };
    } else if (message.content === BotData.DiscordBotPrefix.concat('chess')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'chess').then(async invite => {
                console.log(`${message.member.voice.channel.id}: ${message.content}`);
                return message.reply(`${invite.code}`).then(msg => { setTimeout(() => msg.delete(), 10000) });
            });
        } else { message.reply({ embeds: [notConnected] }); };
    } else if (message.content === BotData.DiscordBotPrefix.concat('betray')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'betrayal').then(async invite => {
                console.log(`${message.member.voice.channel.id}: ${message.content}`);
                return message.reply(`${invite.code}`).then(msg => { setTimeout(() => msg.delete(), 10000) });
            });
        } else { message.reply({ embeds: [notConnected] }); };
    } else if (message.content === BotData.DiscordBotPrefix.concat('doodle')) {
        // If user isnt present in any voice channel
        if (message.member.voice.channel) {
            client.discordTogether.createTogetherCode(message.member.voice.channel.id, 'doodlecrew').then(async invite => {
                console.log(`${message.member.voice.channel.id}: ${message.content}`);
                return message.reply(`${invite.code}`).then(msg => { setTimeout(() => msg.delete(), 10000) });
            });
        } else { message.reply({ embeds: [notConnected] }); };
    };
});

// Login with bot token
client.login(BotData.DiscordBotToken);