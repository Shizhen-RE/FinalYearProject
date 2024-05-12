from typing import List

import os
import random
import string
import time
import csv
import uuid
from bson import ObjectId, json_util
from datetime import timedelta
import logging
import argparse

from pymongo import MongoClient

from recommender.models.topic_based_recommender import TopicBasedRecommender
import recommender.config as config

logging.basicConfig(level=logging.DEBUG, format='%(asctime)s %(levelname)s (%(name)s): %(message)s', datefmt='%Y-%m-%d %H:%M:%S')
logger = logging.getLogger(__name__)

# input
DATA_SOURCE = "topic_messages.csv"

# mongodb connection
DB_URI = config.DB_URI
DB_NAME = "test"

# output
TOPICS_FOLDER = "testdata/topics/" 
MESSAGES_FOLDER = "testdata/messages/" 
REGIONS_FOLDER = "testdata/regions/" 
USERS_FOLDER = "testdata/users/" 

# param
RECOMMENDATION_COUNT = 10 # max number of topic recommendations

# mongo db connection
client = MongoClient(DB_URI)
db = client[DB_NAME]

def import_topic_messages(src: str, output: bool = True) -> dict:
    """
    Import topic-message pairs from csv
    
    params:
        - src: source csv data path
        - output: True if outputing generated documents to file

    return:
        Dict of {topic_id: [message_id]}
    """

    # parse csv, create dict of {topic_name: [message_contents]}
    topic_messages = {}
    with open(src, 'r') as input_csv:
        reader = csv.reader(input_csv, delimiter=',')
        for row in reader:
            topic = row[0]
            message = row[1]
            if topic not in topic_messages:
                topic_messages[topic] = []
            topic_messages[topic].append(message)

    # create topic and message documents
    topic_list = []
    message_list = []
    topic_message_list = {} # dict of {topic_id: [message_id]}
    for topic_name in topic_messages.keys():
        # create topic
        topic_id = str(uuid.uuid4().hex)[:24]
        topic = {
                    "_id": ObjectId(topic_id), 
                    "Name": topic_name,
                    "MessageCount": len(topic_messages[topic_name]),
                    "SubscriptionCount": random.randrange(1000)
                }
        topic_list.append(topic)
        topic_message_list[topic_id] = []

        # create messages in this topic
        for message_text in topic_messages[topic_name]:
            message_id = str(uuid.uuid4().hex)[:24]
            message =   {
                            "_id": ObjectId(message_id),
                            "Geomesh": 0,
                            "Location": [ 0.0, 0.0, 0.0 ],
                            "Type": 'text',
                            "Anchored": 'false',
                            "Size": [ 0, 0 ],
                            "Content": message_text,
                            "Preview": 'null',
                            "Topic": topic_id,
                            "User": ''.join(random.choices(string.digits, k=24)),
                            "Timestamp": 1665946041,
                            "Deleted": 'false',
                            "Likes": 0
                        }
            message_list.append(message)
            topic_message_list[topic_id].append(message_id)
            
    # output documents to file
    if output:
        # make folder for output files
        if not os.path.exists(TOPICS_FOLDER):
            os.makedirs(TOPICS_FOLDER)

        if not os.path.exists(MESSAGES_FOLDER):
            os.makedirs(MESSAGES_FOLDER)

        # write topic documents to json
        for i in range(len(topic_list)):
            outfile = TOPICS_FOLDER + str(topic_list[i]["_id"]) + ".json"
            with open(outfile, 'w') as f:
                f.write(json_util.dumps(topic_list[i], indent=4))
        
        # write message documents to json
        for i in range(len(message_list)):
            outfile = MESSAGES_FOLDER + str(message_list[i]["_id"]) + ".json"
            with open(outfile, 'w') as f:
                f.write(json_util.dumps(message_list[i], indent=4))
    
    # insert topic documents to db
    try:
        db['Topic'].insert_many(topic_list)
    except:
        logger.error("Failed to insert topics to db.")
    
    # insert message documents to db
    try:
        db['Message'].insert_many(message_list)
    except:
        logger.error("Failed to insert messages to db.")

    return topic_message_list

def create_region(topics: List[str], output:bool = True) -> str:
    # create the region document
    region_id = str(uuid.uuid4().hex)[:24]
    region_doc = {
                    "_id": ObjectId(region_id),
                    "Name": "testregion",
                    "Topics": topics
                 }
    
    # write region document to file
    if output:
        if not os.path.exists(REGIONS_FOLDER):
            os.makedirs(REGIONS_FOLDER)

        outfile = REGIONS_FOLDER + region_id + ".json"
        with open(outfile, 'w') as f:
            f.write(json_util.dumps(region_doc, indent=4))

    # insert region document to db
    try:
        db['Region'].insert_one(region_doc)
    except:
        logger.error("Failed to insert region to db.")
    
    return region_id

def create_user(topics: List[str], output:bool = True) -> str:
    # generate random user subscription
    subscription_count = random.randrange(50)
    random_topics = random.sample(topics, k=subscription_count)
    subscription_list = []
    for topic_id in random_topics:
        subscription_list.append(topic_id)

    # create user document
    user_id = str(uuid.uuid4().hex)[:24]
    user_doc = {
        "_id": ObjectId(user_id),
        "Name": "testuser",
        "AuthLocations": [],
        "Subscriptions": subscription_list,
        "Publications": []
    }

    # write user document to file
    if output:
        if not os.path.exists(USERS_FOLDER):
            os.makedirs(USERS_FOLDER)

        outfile = USERS_FOLDER + user_id + ".json"
        with open(outfile, 'w') as f:
            f.write(json_util.dumps(user_doc, indent=4))

    # insert user document to db
    try:
        db['User'].insert_one(user_doc)
    except:
        logger.error("Failed to insert user to db.")
    
    return user_id

def get_filenames(folder: str) -> List[str]:
    filepaths = os.listdir(folder)
    return [f.split('.')[0] for f in filepaths if f.endswith(".json")]

def insert_all_from_folder(folder: str, db_name: str):
    json_objects = []
    for f in os.listdir(folder):
        if f.endswith(".json"):
            with open(folder+f, 'r') as json_stream:
                json_objects.append(json_util.loads(json_stream.read()))

    try:
        db[db_name].insert_many(json_objects)
    except:
        logger.error(f"Failed to insert all to db from folder: {folder}")

def cleanup():
    db['Topic'].delete_many({})
    db['Message'].delete_many({})
    db['User'].delete_many({})
    db['Region'].delete_many({})

def main(clean: bool):
    # check whether testdata folder exists:
    if ((not os.path.exists(MESSAGES_FOLDER)) or 
        (not os.path.exists(TOPICS_FOLDER)) or 
        (not os.path.exists(REGIONS_FOLDER)) or 
        (not os.path.exists(USERS_FOLDER))):

        # import topic and message data from csv
        topic_message_dict = import_topic_messages(src=DATA_SOURCE)
        topic_list = list(topic_message_dict.keys())

        # create a region with all the imported topics
        region_id = create_region(topics=topic_list)

        # create a user with random topic subscriptions from the list
        user_id = create_user(topics=topic_list)
    else:
        # get the existing region_id and user_id
        testregion = db.Region.find_one({"Name": "testregion"})
        if testregion is not None:
            region_id = str(testregion["_id"])
        else:
            region_id = get_filenames(REGIONS_FOLDER)[0]

        testuser = db.User.find_one({"Name": "testuser"})
        if testuser is not None:
            user_id = str(testuser["_id"])
        else:
            user_id = get_filenames(USERS_FOLDER)[0]
            
        logger.info(f"Retrieved region_id={region_id} and user_id={user_id} from files.")

        # check if test db has been cleaned up before
        region_doc = db["Region"].find_one({"_id":ObjectId(region_id)})
        if region_doc is None:
            # insert all documents to test db
            insert_all_from_folder(folder=REGIONS_FOLDER, db_name="Region")
            insert_all_from_folder(folder=USERS_FOLDER, db_name="User")
            insert_all_from_folder(folder=TOPICS_FOLDER, db_name="Topic")
            insert_all_from_folder(folder=MESSAGES_FOLDER, db_name="Message")
            logger.info("Inserted documents to db from testdata folder.")

    # create recommender instance
    recommender = TopicBasedRecommender(DB_URI=DB_URI, DB_name=DB_NAME)

    # make topic-based recommendation for this user's subscriptions
    start_time = time.time()
    recommendations = recommender.recommend(user_id=user_id, region_id=region_id, count=RECOMMENDATION_COUNT)
    end_time = time.time()

    logger.info("Recommended topics IDs:")
    for i in recommendations:
        logger.info(i)
    logger.info("----------------------------------")
    logger.info(f"Recommendation finished in {timedelta(seconds=(end_time - start_time))}")

    # cleanup
    if clean: cleanup()

if __name__ == "__main__":
    # get command line arguments
    parser = argparse.ArgumentParser()
    parser.add_argument('--database', dest='database_uri', help='MongoDB URI with credentials', type=str)
    parser.add_argument('--data_path', dest='data_path', help='Source csv path for the topic-message pairs', type=str)
    parser.add_argument('--no_cleanup', dest='cleanup', help='Cleaning up the test db or not', action='store_false')
    parser.add_argument('--num_result', dest='recommendation_count', help='Max number of recommended topics in output', type=int)
    parser.set_defaults(cleanup=True)
    args = parser.parse_args()

    if args.database_uri is not None:
        DB_URI = args.database_uri
    
    if args.data_path is not None:
        DATA_SOURCE = args.data_path
    
    if args.recommendation_count is not None:
        RECOMMENDATION_COUNT = args.recommendation_count

    logger.info("---------------------------------")
    logger.info(f"DB_URI: {DB_URI}")
    logger.info(f"DATA_SOURCE: {DATA_SOURCE}")
    logger.info(f"Test set to recommend maximum {RECOMMENDATION_COUNT} topic(s) by topic similarity.")
    logger.info("----------------------------------")

    main(clean=args.cleanup) 
