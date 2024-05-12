# syntax=docker/dockerfile:1
FROM ubuntu:latest
VOLUME /tmp

# Install OpenJDK-18
RUN apt-get update && \
    apt-get install -y openjdk-18-jdk && \
    apt-get install -y openjdk-18-jre && \
    apt-get clean;

# Setup JAVA_HOME
ENV JAVA_HOME=/usr/lib/jvm/java-18-openjdk-amd64
ENV PATH=$PATH:$JAVA_HOME/bin

COPY nearme_backend.jar appServer.jar
COPY nearmefirebase-firebase-adminsdk-8agns-1808c2842b.json nearmefirebase-firebase-adminsdk-8agns-1808c2842b.json
ENV GOOGLE_APPLICATION_CREDENTIALS=nearmefirebase-firebase-adminsdk-8agns-1808c2842b.json
ENTRYPOINT [ "java", "-Djava.security.egd=file:/dev/./urandom", "-jar", "appServer.jar" ]
