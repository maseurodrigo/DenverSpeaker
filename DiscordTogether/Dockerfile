# syntax=docker/dockerfile:1
# node:16.10.0
FROM node:16-alpine3.14

# Create app directory
ENV NODE_ENV=production
WORKDIR /app

# Install app dependencies
RUN npm install -g npm@8.1.0
RUN npm install discord-together@latest
RUN npm install discord.js@latest

# Bundle app source
COPY ./DiscordTogether/ .
COPY ./BotData.json BotData.json

# Internal port expose
EXPOSE 8080

CMD ["node", "run.js"]