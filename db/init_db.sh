#!/bin/sh

# This script is to initialize mongodb in docker container with backup json data
# Must have mongo shell installed to run this script

mongo_host=$1

if [ ! -d "./backup" ]; then
    unzip backup.zip
fi

cd backup
for d in *; do
    cd $d
    for f in *; do
        file_path="/backup/$d/$f" # this should be abs path to the file in the container
        collection="${f%.*}"
        mongoimport mongodb://$mongo_host:27017 --db $d --collection $collection --type json --jsonArray --file $file_path
    done
    cd ..
done
