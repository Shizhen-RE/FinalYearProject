# syntax=docker/dockerfile:1

# This file is for seeding the mongodb with backup data and init_db.sh in docker-compose
FROM mongo:latest

COPY . .

# initialize db, where the mongo host is the mongodb instance in the docker-compose stack
CMD ./init_db.sh mongodb
