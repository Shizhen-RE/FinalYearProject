import string
import random
import time
import requests
import csv
import argparse

from pymongo import MongoClient

SERVER_URL = "http://localhost:8080/"
ADD_MSG_URL = SERVER_URL + "message"
ADD_TOPIC_URL = SERVER_URL + "addTopic"

DATA_PATH = ""

REGION = "63dd7b834f2edcf2226a5039" # region id for 'Waterloo,ON,CA'
TEST_USER = "udSGwJxV7JbyMajxeZjvTVqSWW93" # user@example.com

DB_URI = "mongodb+srv://aidanfo:50VsPsP5MSUJG8AG@near-me.c9eaw.mongodb.net/?retryWrites=true&w=majority&authSource=admin"
DB_NAME = "Main"

# mongo db connection
client = MongoClient(DB_URI)
db = client[DB_NAME]


class NearMeMessage:
    def __init__(
                    self,
                    user, 
                    topic,
                    coordinates,
                    timestamp,
                    likes,
                    isLiked,
                    content,
                    isAR,
                    imageFormat,
                    preview,
                    style,
                    scale,
                    color = None,
                    size = None,
                    rotation = None
        ):
        self.User = user
        self.Topic = topic
        self.Coordinates = coordinates
        self.Timestamp = timestamp
        self.Content = content
        self.IsAR = isAR
        self.Size = size
        self.Likes = likes
        self.IsLiked = isLiked
        self.ImageFormat = imageFormat
        self.Preview = preview
        self.Color = color
        self.Style = style
        self.Scale = scale
        self.Rotation = rotation

    def get(self):
        return {
            "User": self.User,
            "Topic": self.Topic,
            "Coordinates": self.Coordinates,
            "Timestamp": self.Timestamp,
            "Likes": self.Likes,
            "IsLiked": self.IsLiked,
            "Content": self.Content,
            "IsAR": self.IsAR,
            "Size": self.Size,
            "ImageFormat": self.ImageFormat,
            "Preview": self.Preview,
            "Color": self.Color,
            "Style": self.Style,
            "Scale": self.Scale,
            "Rotation": self.Rotation
        }

def main():
    # geo coordinates for the Waterloo area
    minLat = 43.4
    maxLat = 43.5
    minLon = -80.6
    maxLon = -80.45

    # read data from csv
    msg_list = []
    topic_msg_count = {}
    with open(DATA_PATH, 'r') as input_csv:
        reader = csv.reader(input_csv, delimiter=',')
        for row in reader:
            topic = row[0]
            message = row[1]
            lat = row[2]
            lon = row[3]

            if lat is "": lat = random.uniform(minLat, maxLat)
            if lon is "": lon = random.uniform(minLon, maxLon)

            msg_list.append({
                "topic": topic,
                "content": message,
                "coordinates":[lat, lon, 0.0]
            })

            print(row, lat, lon)

            if topic in topic_msg_count:
                topic_msg_count[topic] += 1
            else:
                topic_msg_count[topic] = 0
    
    # add topics to db via API requests
    for t in topic_msg_count:
        params = {
            "name": t,
            "regionName": "Waterloo,ON,CA"
        }

        # using fakebearertoken will add topic to user 'user@example.com'
        res = requests.get(url=ADD_TOPIC_URL, params=params, headers={"Authorization": "fakebearertoken"})
        if res.status_code != 200:
            print(f"ERROR: adding topic {t}")
            print(res.json())


    # create message requests
    msg_req_list = []
    for m in msg_list:
        # get topic id from db
        topic_id = db.Topic.find_one({"Name": m["topic"], "RegionId": REGION})["_id"]

        # create the message request body
        msg_req_list.append(
            NearMeMessage(
                topic=str(topic_id),
                user=TEST_USER,
                coordinates=m["coordinates"],
                timestamp=int(time.time()),
                likes=0,
                isLiked=False,
                content=m["content"],
                isAR=False,
                imageFormat="",
                preview="",
                style="",
                scale=0.0
            )
        )

    # post all the messages to the server to add to db
    for i, msg_req in enumerate(msg_req_list):
        res = requests.post(url=ADD_MSG_URL, headers={"Authorization": "fakebearertoken"}, json=msg_req.get())
        if res.status_code != 200:
            print(f"ERROR: post {i}th message")
            print(res.json())

if __name__ == "__main__":

    parser = argparse.ArgumentParser()
    parser.add_argument('--datacsv', dest='data_path', help='import data path', type=str)
    args = parser.parse_args()

    if args.data_path is not None:
        DATA_PATH = args.data_path

    main()
