# syntax=docker/dockerfile:1
# openjdk:19-latest
FROM azul/zulu-openjdk:19-latest
WORKDIR /opt/Lavalink

# Run as non-root user
RUN groupadd -g 322 lavalink && useradd -r -u 322 -g lavalink lavalink
USER lavalink

# Copy Lavalink.jar
COPY ./Lavalink/Lavalink.jar Lavalink.jar
COPY ./Lavalink/application.yml application.yml

# Internal port expose
EXPOSE 2333

ENTRYPOINT ["java", "-Djdk.tls.client.protocols=TLSv1.1,TLSv1.2", "-jar", "Lavalink.jar"]