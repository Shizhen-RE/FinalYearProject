import os
import argparse
from bson import json_util

from pymongo import MongoClient

DB_URI = "mongodb://127.0.0.1:27017"
DATA_FOLDER = "./backup"

def main():
    # import data from backup
    client = MongoClient(DB_URI)
    folders = os.listdir(DATA_FOLDER)
    for folder_name in folders:
        db = client[folder_name]
        folder_path = '/'.join([DATA_FOLDER, folder_name])
        files = os.listdir(folder_path)
        for file_name in files:
            file_path = '/'.join([folder_path, file_name])

            # read collection content from json file
            json_list = []
            with open(file_path, 'r') as infile:
                json_list = json_util.loads(infile.read())

            # insert all to collection
            try:
                collection = db[folder_name]
                collection.insert_many(json_list)
            except Exception as e:
                print(f"ERROR: Failed to insert data to {folder_name}.{file_name}: " + str(e))


if __name__ == "__main__":
    # get command line arguments
    parser = argparse.ArgumentParser()
    parser.add_argument('--database', dest='database_uri', help='MongoDB URI', type=str)
    parser.add_argument('--data_path', dest='data_path', help='Data folder path', type=str)
    args = parser.parse_args()

    if args.database_uri is not None:
        DB_URI = args.database_uri
    if args.data_path is not None:
        DATA_FOLDER = args.data_path

    main()
