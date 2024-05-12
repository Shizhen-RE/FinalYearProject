from typing import List
from bson import ObjectId

import logging

from pymongo import MongoClient

if __package__ == "recommender.recommender.models":
    import recommender.recommender.config as config
else:
    # when calling from test folder
    import recommender.config as config
    
logger = logging.getLogger(__name__)

class UserBasedRecommender:

    def __init__(self, DB_URI:str, DB_name: str) -> None:
        # URI to mongoDB
        self.db1_URI = DB_URI

        # connect to mongodb 
        self.client = MongoClient(self.db1_URI)
        self.db1 = self.client[DB_name] # main db
        self.db2 = self.client[config.RECOMMENDER_DB]

    def __del__(self) -> None:
        self.client.close()

    def find_similar_users(self, user: dict, count: int = 20) -> List[dict]:
        """
        Get the list of similar users based on the current user's topic subscriptions.

        params:
            - user: the user document in mongodb.
            - count: maximum number of similar users to be returned, default 20.

        return:
            - List of similar users [{"_id": user_id, "Score": similarity_score, "Subscriptions": [topic_ids]}], sorted by similarity score from high to low.
        """

        # get the subscriptions for the current user
        subscriptions_list = user["Subscriptions"]

        # aggregation pipeline to find similar users by the current user's subscriptions
        # similarity score is the percentage of matching topics between two subscription lists
        pipeline = [
            {
                "$unwind": "$Subscriptions",
            },
            {
                "$match": { "Subscriptions": { "$in": subscriptions_list } },
            },
            {
                "$group": {
                    "_id": '$_id',
                    "count": { "$sum": 1 },
                },
            },
            {
                "$limit": count
            },
            {
                "$project": {
                    "_id": 1,
                    "count": 1,
                    "Score": { "$divide": ["$count", len(subscriptions_list)] },
                },
            },
            {
                "$sort": { "Score": -1 },
            }
        ]

        # find similar users as a list of dictionaries {Name:str, Score:float}
        similar_users = list(self.db1.User.aggregate(pipeline))

        # remove self from similar users
        similar_users = [u for u in similar_users if u["_id"] != user["_id"]]
        if len(similar_users) == 0:
            return []
        
        # append subscriptions list for the similar users
        for u in similar_users:
            user_doc = self.db1.User.find_one({"_id" : u["_id"]})
            u["Subscriptions"] = user_doc["Subscriptions"]
        
        # update cache
        logger.info(f"Updating similar user cache for user {user['_id']}")
        self.db2[config.UB_CACHE_COLLECTION].update_one(
            {"user_id": str(user["_id"])},
            {"$set": 
                {
                    "user_id": str(user["_id"]),
                    "similar_users": similar_users
                }
            },
            upsert=True
        )

        # return list of similar users with their similarity scores
        return similar_users


    def recommend(self, user_id:str, region_id:str, result_count:int = 10, user_count:int = 20, compute: bool = False, verbose:bool = True) -> List[str]:
        """
        Get recommended topics for a user by collaborative filtering.

        params:
            - user_id: "_id" of user in db.
            - region_id: "_id" of the user's current geographicall region in db.
            - result_count: number of recommended topics to be returned.
            - user_count: number of similar users to be used for collaborative filtering.
            - compute: if forced to re-compute the similar users
            - verbose: print recommendation details if verbose==True.
        return:
            - List of topic IDs (object_id in db).
        """

        # find user in the database
        user_doc = self.db1.User.find_one({"_id" : ObjectId(user_id)})
        if verbose:
            logger.info(f"Recommending for user: {user_id}")

        # find the similar users by topic subscription
        if compute:
            logger.info(f"Computing similar users for user {user_id}")
            similar_users = self.find_similar_users(user=user_doc, count=user_count)
        else:
            # check cache
            user_cache = self.db2[config.UB_CACHE_COLLECTION].find_one({"user_id": user_id})
            if user_cache is None:
                logger.info(f"Cache not found, computing similar users for user {user_id}")
                similar_users = self.find_similar_users(user=user_doc, count=user_count)
            else:
                logger.info(f"Found similar user cache for user {user_id}")
                similar_users = user_cache["similar_users"]

        if verbose:
            logger.info(f"Found {len(similar_users)} similar users: ")
            if len(similar_users) > 0:
                logger.info("{:<28} {:<20}".format("User ID", "Score"))
                for user in similar_users:
                    logger.info("{:<28} {:<20}".format(str(user["_id"]), str(user["Score"])))

        # get the subscriptions for the current user
        subscriptions_list = user_doc["Subscriptions"]

        # get the most recommended topics by counting their occurence frequency in the similar users' subscription list
        topic_frenquencies = {}
        for user in similar_users:
            for topic in user["Subscriptions"]:
                if not topic in topic_frenquencies:
                    topic_frenquencies[topic] = 1
                else:
                    topic_frenquencies[topic] += 1
        
        # get the topics available in the current region
        region_doc = self.db1.Region.find_one({"_id" : ObjectId(region_id)})
        region_topic_list = region_doc["Topics"]

        # filter the recommended topics by region and remove user's current subscriptions
        for topic in list(topic_frenquencies.keys()):
            if (topic in subscriptions_list) or (topic not in region_topic_list):
                topic_frenquencies.pop(topic)

        # sort the topic list by the frequency
        sorted_topic_list = sorted(topic_frenquencies.items(), key=lambda kv: kv[1], reverse=True)
        if len(sorted_topic_list) > result_count:
            sorted_topic_list = sorted_topic_list[:result_count]

        if verbose:
            logger.info("Topic scores:")
            logger.info("{:<28} {:<28} {:<18}".format("topic ID", "topic name", "frequency"))
            for topic_id, value in dict(sorted_topic_list).items():
                topic_name = self.db1['Topic'].find_one({"_id" : ObjectId(topic_id)})["Name"]
                logger.info("{:<28} {:<28} {:<18}".format(topic_id, topic_name, value))

        # return the top few recommended topics
        return list(dict(sorted_topic_list).keys())
