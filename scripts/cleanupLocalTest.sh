#!/bin/bash

# This script is to terminate the recommender app and the spring-boot server for local test

SCRIPT="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
DIR="$(dirname "$SCRIPT")"

echo "Terminating recommender..."
kill $(lsof -t -i:9000)

echo "Terminating server..."
kill $(lsof -t -i:8080)

# recover URLs
sed -i.backup 's/localhost:9000/recommender-service:9000/' "$DIR"/../NearMe/nearme_backend/src/main/java/com/nearme/nearme_backend/service/RecommenderServiceImpl.java
rm "$DIR"/../NearMe/nearme_backend/src/main/java/com/nearme/nearme_backend/service/RecommenderServiceImpl.java.backup

echo "Done."
