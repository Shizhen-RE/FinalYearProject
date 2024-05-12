import os
import random
import string
import time
from typing import List
from bson import ObjectId, json_util
from datetime import timedelta
import argparse
import logging

import uuid
from pymongo import MongoClient

from recommender.models.user_based_recommender import UserBasedRecommender
import recommender.config as config

logging.basicConfig(level=logging.DEBUG, format='%(asctime)s %(levelname)s (%(name)s): %(message)s', datefmt='%Y-%m-%d %H:%M:%S')
logger = logging.getLogger(__name__)

TOPICS_FOLDER = "testdata/topics/" 
USERS_FOLDER = "testdata/users/" 
REGIONS_FOLDER = "testdata/regions/"

TOPIC_COUNT = 100
USER_COUNT = 10000
RECOMMENDATION_COUNT = 10
SIMILAR_USER_COUNT = 20

DB_URI = config.DB_URI
DB_NAME = "test"

# mongo db connection
client = MongoClient(DB_URI)
db = client[DB_NAME]

def generate_topics(count: int, output: bool) -> List[str]:
    """Generate random topics with fake data"""
    if count <= 0: return []

    topic_list = []
    for _ in range(count):
        # generate topic name with 20 random alphanumeric characters
        topic_name = ''.join(random.choices(string.ascii_letters + string.digits, k=20))

        # create topic document
        id = str(uuid.uuid4().hex)[:24]
        topic = {
                    "_id": ObjectId(id), 
                    "Name": topic_name,
                    "MessageCount": random.randrange(10000),
                    "SubscriptionCount": random.randrange(1000)
                }
        topic_list.append(topic)

    if output:
        # make folder for output files
        if not os.path.exists(TOPICS_FOLDER):
            os.makedirs(TOPICS_FOLDER)

        for i in range(len(topic_list)):
            outfile = TOPICS_FOLDER + str(topic_list[i]["_id"]) + ".json"

            # write topic document to a json file
            with open(outfile, 'w') as f:
                f.write(json_util.dumps(topic_list[i], indent=4))
    
    # insert topics to db
    try:
        db['Topic'].insert_many(topic_list)
    except:
        logger.error("Failed to insert topics to db.")

    return [str(topic["_id"]) for topic in topic_list]

def generate_users(count: int, topics: List[str], output: bool) -> List[str]:
    """Generate random users with fake data"""
    if count <= 0: return []

    user_list = []
    for _ in range(count):
        # generate user names with 15 random alphanumeric characters
        user_name = ''.join(random.choices(string.ascii_letters + string.digits, k=15))
        
        # generate random count of subscriptions list for user
        subscription_count = random.randrange(len(topics)-1)
        subscription_list  = random.sample(topics, k=subscription_count)
        
        # create user document
        id = str(uuid.uuid4().hex)[:24]
        user = {
                    "_id": ObjectId(id), 
                    "Name": user_name,
                    "AuthLocations": [],
                    "Subscriptions": subscription_list,
                    "Publications": []
                }
        user_list.append(user)

    if output:
        # make folder for output files
        if not os.path.exists(USERS_FOLDER):
            os.makedirs(USERS_FOLDER)

        for i in range(len(user_list)):
            outfile = USERS_FOLDER + str(user_list[i]["_id"]) + ".json"

            # write user document to a json file
            with open(outfile, 'w') as f:
                f.write(json_util.dumps(user_list[i], indent=4))
    
    # insert users to db
    try:
        db['User'].insert_many(user_list)
    except:
        logger.error("Failed to insert users to db.")

    return [str(user["_id"]) for user in user_list]

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

def cleanup():
    db['User'].delete_many({})
    db['Topic'].delete_many({})
    db['Region'].delete_many({})

def get_filenames(folder: str) -> List[str]:
    filepaths = os.listdir(folder)
    return [f.split('.')[0] for f in filepaths if f.endswith(".json")]

def main(clean: bool):
    # generate random topics if no testdata exists
    if not os.path.exists(TOPICS_FOLDER):
        topic_id_list = generate_topics(count=TOPIC_COUNT, output=True)
    else:
        all_topic_ids = get_filenames(TOPICS_FOLDER)
        diff = len(all_topic_ids) - USER_COUNT
        if diff <= 0:
            topic_id_list = all_topic_ids
            topic_id_list.extend(generate_topics(count=-diff, output=True))
        elif diff > 0:
            topic_id_list = random.sample(all_topic_ids, diff)

    # generate random users if no testdata exists
    if not os.path.exists(USERS_FOLDER):
        user_id_list = generate_users(count=USER_COUNT, topics=topic_id_list, output=True)
    else:
        all_user_ids = get_filenames(USERS_FOLDER)
        diff = len(all_user_ids) - USER_COUNT
        if diff <= 0:
            user_id_list = all_user_ids
            user_id_list.extend(generate_users(count=-diff, topics=topic_id_list, output=True))
        elif diff > 0:
            user_id_list = random.sample(all_user_ids, diff)

    # generate a region with all topics if no testdata exists
    if not os.path.exists(REGIONS_FOLDER):
        region_id = create_region(topics=topic_id_list, output=True)
    else:
        region_id = get_filenames(REGIONS_FOLDER)[0]

    # pick a random user
    user_id = random.choice(user_id_list)
    logger.info("Random user ID: {user_id}")

    # test recommender, get default 10 recommended topics with 20 similar users
    recommender = UserBasedRecommender(DB_URI=DB_URI, DB_name=DB_NAME)
    start_time = time.time()
    recommendations = recommender.recommend(user_id=user_id, region_id=region_id, result_count=RECOMMENDATION_COUNT, user_count=SIMILAR_USER_COUNT, verbose=True)
    end_time = time.time()

    logger.info("Recommendeded topic IDs:")
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
    parser.add_argument('--num_users', dest='user_count', help='Number of users in the test', type=int)
    parser.add_argument('--num_topics', dest='topic_count', help='Number of topics in the test', type=int)
    parser.add_argument('--num_similar', dest='similar_user_count', help='Max number of similar users in the collaborative filtering', type=int)
    parser.add_argument('--num_result', dest='recommendation_count', help='Max number of recommended topics in output', type=int)
    parser.add_argument('--no_cleanup', dest='cleanup', help='Cleaning up the test db or not', action='store_false')
    args = parser.parse_args()

    if args.database_uri is not None:
        DB_URI = args.database_uri
    
    if args.user_count is not None:
        USER_COUNT = args.user_count
    
    if args.topic_count is not None:
        TOPIC_COUNT = args.topic_count

    if args.similar_user_count is not None:
        SIMILAR_USER_COUNT = args.similar_user_count

    if args.recommendation_count is not None:
        RECOMMENDATION_COUNT = args.recommendation_count

    logger.info("----------------------------------")
    logger.info(f"DB_URI: {DB_URI}")
    logger.info(f"USER_COUNT: {USER_COUNT}")
    logger.info(f"TOPIC_COUNT: {TOPIC_COUNT}")
    logger.info(f"Test set to recommend maximum {RECOMMENDATION_COUNT} topic(s) by collaborative filtering with {SIMILAR_USER_COUNT} users")
    logger.info("----------------------------------")

    main(clean=args.cleanup) 