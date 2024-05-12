from typing import List
from bson import ObjectId
import time
from datetime import timedelta, datetime
import logging

from pymongo import MongoClient
from apscheduler.schedulers.background import BackgroundScheduler

from . import config as config
from .models.topic_based_recommender import TopicBasedRecommender
from .models.user_based_recommender import UserBasedRecommender

# recommender models
tb_rec = TopicBasedRecommender(DB_URI=config.DB_URI, DB_name=config.MAIN_DB)
ub_rec = UserBasedRecommender(DB_URI=config.DB_URI, DB_name=config.MAIN_DB)

# mongodb
client = MongoClient(config.DB_URI)
main_db = client[config.MAIN_DB]
rec_db = client[config.RECOMMENDER_DB]

# logging
logger = logging.getLogger(__name__)

def check_user_increase():
    """
    Task to re-compute similar users when total user count increases by 10%
    """
    logger.info("Checking total user increase.")
    # get total user count
    user_count = main_db.User.count_documents({})

    # check previous count
    meta_cache = rec_db[config.META_COLLECTION].find_one({"type": config.USER_COUNT_TYPE})
    if meta_cache is None:
        previous_count = 0
    else:
        previous_count = meta_cache["count"]

    # update meta data
    current_timestamp = time.time() # current UTC timestamp
    rec_db[config.META_COLLECTION].update_one(
        {"type": config.USER_COUNT_TYPE},
        {"$set": 
            {
                "type": config.USER_COUNT_TYPE,
                "count": user_count,
                "timestamp": current_timestamp
            }
        },
        upsert=True
    )

    if user_count > (config.USER_COUNT_MULT * previous_count):
        logger.info("Updating similar user cache.")
        compute_similar_users()
        logger.info("Finished updating similar user cache.")
    
    logger.info("Finished checking total user increase.")

def check_message_increase():
    """
    Task to re-sample message and compute topic encoding when total message count increase by 10% in a region
    """
    logger.info("Checking total message increase.")
    regions = list(main_db.Region.find({}))
    region_id_list = []
    for region in regions:
        if check_message_increase_in_region(region):
            region_id_list.append(region["_id"])

    # TODO: multithread re-compute similar topics for region
    for region_id in region_id_list:
        logger.info(f"Updating similar topics cache for region {region_id}")
        compute_similar_topics(region_id)
        logger.info(f"Finished updating similar topics cache for region {region_id}")

    logger.info("Finished checking total message increase.")

def check_message_increase_in_region(region: dict):
    topic_ids = region["Topics"]
    message_count = 0
    for topic_id in topic_ids:
        message_count += main_db.Message.count_documents({"Topic": topic_id})
    
    # check previous count
    meta_cache = rec_db[config.META_COLLECTION].find_one({"region_id": region["_id"]})
    if meta_cache is None:
        previous_count = 0
    else:
        previous_count = meta_cache["count"]

    # update meta data
    current_timestamp = time.time() # current UTC timestamp
    rec_db[config.META_COLLECTION].update_one(
        {"region_id": region["_id"]},
        {"$set":
            {
                "type": config.MSG_COUNT_TYPE,
                "region_id": region["_id"],
                "count": message_count,
                "timestamp": current_timestamp
            }
        },
        upsert=True
    )

    return message_count > (config.MSG_COUNT_MULT * previous_count)

def compute_similar_users():
    """
    Task to update similar users for a certain user at a regular interval.
    """
    user_list = list(main_db.User.find({}))

    for user in user_list:
        ub_rec.find_similar_users(user) # updates cache

def compute_similar_topics(region_id: str):
    """
    Task to update similar topics for all topics in a region at a regular interval.
    """
    region_doc = main_db.Region.find_one({"_id": ObjectId(region_id)})
    for topic_id in region_doc["Topics"]:
        tb_rec.find_similar_topics(target_topic_id=topic_id, region_id=region_id) # updates cache

def encode_messages_in_regions():
    """
    Task to sample and encode messages for each region at a regular interval.
    """
    regions = list(main_db.Region.find({}))
    region_id_list = [str(region["_id"]) for region in regions]

    for region_id in region_id_list:
        logger.info(f"Encoding messages for region {region_id}.")
        tb_rec.encode_messages_in_region(region_id=region_id, compute=True)
        logger.info(f"Finished encodind messages for region {region_id}.")

def get_recommendations(user_id: str, region_id: str) -> List[str]:
    """
    Responds to the getRecommendation API.
    """
    start_time = time.time()

    # get the user-based recommendations
    ub_rec_topics = ub_rec.recommend(user_id=user_id, region_id=region_id, result_count=10)

    # get the topic-based recommendations
    tb_rec_topics = tb_rec.recommend(user_id=user_id, region_id=region_id, count=10)

    # result list is the intersection of the two sets of topic ids
    result_list = ub_rec_topics + list(set(tb_rec_topics) - set(ub_rec_topics))
    
    logger.info(f"Recommendation finished in {timedelta(seconds=time.time()-start_time)}.")
    return result_list

def start():
    scheduler = BackgroundScheduler(daemon=True)
    scheduler.add_job(check_message_increase, 'interval', hours=4, next_run_time=datetime.now())
    scheduler.add_job(check_user_increase, 'interval', hours=4, next_run_time=datetime.now())
    scheduler.add_job(encode_messages_in_regions, 'interval', hours=24)
    scheduler.start()
