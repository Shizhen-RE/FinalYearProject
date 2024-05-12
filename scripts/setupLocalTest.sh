#!/bin/bash

# This scripts brings up a backend (server + recommender) for local testing.
# Make sure you have the correct environment setup
# Make sure you have installed all the python dependencies as described in recommender/README.md

SCRIPT="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
DIR="$(dirname "$SCRIPT")"

cleanup_recommender()
{
    echo
    echo "ERROR: Failed to reach recommender at $recommender_app."
    kill $recommender_pid
    exit 1
}

cleanup_backend()
{
    echo
    echo "ERROR: Failed to reach server at $server."
    kill $recommender_pid
    kill $server_pid
    cd "$DIR"/../NearMe/nearme_backend
    # recover URLs
    sed -i 's/localhost:9000/recommende-service:9000/' \
        src/main/java/com/nearme/nearme_backend/service/RecommenderServiceImpl.java
    exit 2
}

recommender_app="http://localhost:9000/"
trap cleanup_recommender SIGINT

echo "Bringing up recommender..."
cd "$DIR"/../recommender
# start the recommender app with default MongoDB Atlas connection
nohup python3 -m flask run --port=9000 > log.txt 2>&1 &
recommender_pid=$!

# check if recommender is up
echo "Checking recommender status..."
while [[ $(curl --write-out '%{http_code}' --silent --output /dev/null $recommender_app) -ne 200 ]]; do
    sleep 1
done
echo "Recommender is up."

server="http://localhost:8080/healthCheck"
trap cleanup_backend SIGINT

echo "Bringing up server..."
cd "$DIR"/../NearMe/nearme_backend/
# replace RECOMMENDATION_URL
sed -i.backup 's/recommender-service:9000/localhost:9000/' src/main/java/com/nearme/nearme_backend/service/RecommenderServiceImpl.java
# build and run
./mvnw clean install -DskipTests
export GOOGLE_APPLICATION_CREDENTIALS=nearmefirebase-firebase-adminsdk-8agns-1808c2842b.json
nohup ./mvnw spring-boot:run > log.txt 2>&1 &
server_pid=$!

# check if server is up
echo "Checking server status..."
while [[ $(curl --write-out '%{http_code}' --silent --output /dev/null $server) -ne 200 ]]; do
    sleep 1
done
echo "Server is up."

echo "Done."
