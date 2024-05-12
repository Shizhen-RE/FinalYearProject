from typing import List, OrderedDict
from bson import ObjectId

import random
import time
import logging

from pymongo import MongoClient
from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity
import numpy as np

if __package__ == "recommender.recommender.models":
    import recommender.recommender.config as config
else:
    # when calling from test folder
    import recommender.config as config

logger = logging.getLogger(__name__)

class TopicBasedRecommender:

    def __init__(self, DB_URI:str, DB_name: str, sample_size: int = config.MSG_SAMPLE_SIZE) -> None:
        # URI to mongoDB
        self.db1_URI = DB_URI

        # connect to mongodb 
        self.client = MongoClient(self.db1_URI)
        self.db1 = self.client[DB_name]  # main database
        self.db2 = self.client[config.RECOMMENDER_DB] # recommender database

        # message sample size
        self.MSG_SAMPLE_SIZE = sample_size

        # model for encoding
        self.model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')
        self.model.max_seq_length = 120

    def __del__(self) -> None:
        self.client.close()

    def _encode_messages(self, messages: List[str]) -> List[str]:
        return self.model.encode(messages)

    def _encode_messages_for_topic(self, topic_id: str, region_id:str = None, compute: bool = False) -> List[str]:
        # try to find the topic in db2 if not forced to compute
        doc = self.db2[config.ENCODER_COLLECTION].find_one({"topic_id" : topic_id})

        # did not find topic in db2 or is forced to compute
        if compute or doc is None:
            # find the topic in db1
            topic = self.db1['Topic'].find_one({"_id" : ObjectId(topic_id)})

            # if topic has less than MSG_SAMPLE_SIZE messages, retrieve them all and sample with replacement
            if topic['MessageCount'] <= 0:
                return []
            elif topic['MessageCount'] < self.MSG_SAMPLE_SIZE:
                # find the messages in topic with non-empty text content
                selected_messages = list(self.db1['Message'].find({"Topic" : topic_id, "Content": {"$gte": " "}}))
                selected_messages = random.choices(selected_messages, k=self.MSG_SAMPLE_SIZE)
            else:
                pipeline =  [
                                {'$match' : {'Topic' : topic_id}},
                                {'$sample' : {'size' : self.MSG_SAMPLE_SIZE}}
                            ]
                selected_messages = list(self.db1['Message'].aggregate(pipeline))
            
            messages = []
            for message in selected_messages:
                messages.append(message['Content'])
            
            # encode messages
            encoded_messages = self._encode_messages(messages)
            
            # get region_id if not provided
            if region_id is None:
                region_id = str(self.db1["Region"].find_one({"Topics": topic_id})["_id"])

            # if topic_id exists in db2 then update document, else insert new
            try:
                key = {"topic_id": topic_id}
                self.db2[config.ENCODER_COLLECTION].update_one(
                    key,
                    {"$set":{
                        "region_id": region_id,
                        "topic_id": topic_id,
                        "encoded_messages": encoded_messages.tolist() # convert from numpy 2d-array
                    }},
                    upsert=True)
            except Exception as e:
                raise Exception(f"ERROR: Cannot update message encodings for topic {topic_id}: " + str(e))
            
            finally:
                return encoded_messages

        else:
            # return cached encodings
            doc["encoded_messages"]

    def encode_messages_in_region(self, region_id: str, compute:bool = False) -> dict:
        """
        For a given region, sample and encode messages from its available topics.

        params
            - region_id: "_id" of region in db
        
        return:
            Dictionary in the form of {topic_id : [encoded_messages]}
        """
        # get the available topics in this region
        region_doc = self.db1["Region"].find_one({"_id" : ObjectId(region_id)})
        topic_id_list = region_doc["Topics"]

        # sample and encode messages from each topic
        start_time = time.time()
        topic_messages = {}
        for topic_id in topic_id_list:
            messages = self._encode_messages_for_topic(topic_id=topic_id, region_id=region_id, compute=compute)

            # add encoded messages to dict
            topic_messages[topic_id] = messages

        logger.info(f"Took {time.time() - start_time}s to encode {len(topic_id_list)} topics in region {region_id}")
        return topic_messages

    def _get_region_embeddings(self, region_id:str) -> dict:
        # validate region_id
        region_doc = self.db1["Region"].find_one({"_id":ObjectId(region_id)})
        if region_doc is None:
            raise Exception("ERROR: Invalid region_id.")

        # get the number of topics for this region in db1 and db2
        db1_list = list(region_doc["Topics"])
        db2_list = list(self.db2[config.ENCODER_COLLECTION].find({"region_id":region_id}))
        
        # if db2 has cached entries for all topics in this region, return cached entries directly
        if len(db1_list) == len(db2_list):
            # convert to dict of the form {topic_id:[encoded_messages]}
            topic_messages = {}
            for doc in db2_list:
                topic_id = doc["topic_id"]
                topic_messages[topic_id] = doc["encoded_messages"]

            return topic_messages
        else:
            # find the region in db1 and encode messages in its topics (will skip the topics that are already cached)
            return self.encode_messages_in_region(region_id, compute=False)

    
    def _get_topic_embeddings(self, topic_id:str) -> List[str]:
        # try to find the topic in db2
        doc = self.db2[config.ENCODER_COLLECTION].find_one({"topic_id" : topic_id})
        if doc is None:
            logger.info(f"Encoding messages for topic {topic_id}")
            return self._encode_messages_for_topic(topic_id, compute=False)
        else:
            logger.info(f"Found encodings cache for topic {topic_id}")
            return doc["encoded_messages"]

            
    def find_similar_topics(self, target_topic_id:str, region_id:str, count:int = 50) -> dict:
        """
        Find topics similar to the given topic in the given region.

        params:
            - target_topic_id: target topic id
            - region_id: "_id" of the region
            - count: max number of similar topics to return
        return: 
            Similar topics as a dict {"topic_id",similarity_score}
        """
        # sample messages from topics in the region and encode
        encoded_topic_messages = self._get_region_embeddings(region_id)
        topic_id_list = encoded_topic_messages.keys()

        # compute similarity scores 
        score_dict = {}

        # get target embedding
        target_embeddings = self._get_topic_embeddings(target_topic_id)
    
        for topic_id in topic_id_list:
            selected_embeddings = encoded_topic_messages[topic_id] 
            score_dict[topic_id] = np.sum(cosine_similarity(target_embeddings, selected_embeddings))
 
        # sort the topics by similarity score
        sorted_topics_list = sorted(score_dict.items(), key=lambda kv: kv[1], reverse=True)

        # return top few similar topics
        if len(sorted_topics_list) > count:
            result_dict = dict(sorted_topics_list[:count])
        else:
            result_dict = dict(sorted_topics_list)

        # update cache
        logger.info(f"Updating similar topic cache for topic {target_topic_id}")
        self.db2[config.TB_CACHE_COLLECTION].update_one(
            {"topic_id": target_topic_id,},
            { "$set":
                {
                    "topic_id": target_topic_id,
                    "similar_topics": result_dict
                }
            },
            upsert=True
        )

        return result_dict


    def recommend(self, user_id: str, region_id: str, count:int = 10, compute:bool = False, verbose:bool = True) -> List[str]:
        """
        Recommend topics by finding similar topics to user's current subscriptions.

        params:
            - user_id: "_id" of user in db, recommend topics for this user's subscriptions
            - region_id: "_id" of region in db, contains the available topics for search
            - count: the maximum number of similar topics to return 
            - compute: if forced to re-compute the similar topics
            - verbose: True if logger.infoing is enabled

        return:
            List of recommended topic IDs.
        """
        # find user in the database
        user_doc = self.db1.User.find_one({"_id" : ObjectId(user_id)})
        if verbose:
            logger.info(f'Recommending topics for user: {user_doc["_id"]}')
        
        # find similar topics to the user's subscriptions
        subscriptions_list = user_doc["Subscriptions"]
        similar_topics_scores = {}
        for subscribed_topic_id in subscriptions_list:
            if compute:
                logger.info(f"Computing similar topics for topic {subscribed_topic_id} in region {region_id}.")
                similar_topics = self.find_similar_topics(subscribed_topic_id, region_id=region_id, count=100)
            else:
                # check cache
                topic_cache = self.db2[config.TB_CACHE_COLLECTION].find_one({"topic_id": subscribed_topic_id})
                if topic_cache is None:
                    logger.info(f"Cache not found, computing similar topics for topic {subscribed_topic_id} in region {region_id}.")
                    similar_topics = self.find_similar_topics(subscribed_topic_id, region_id=region_id, count=100)
                else:
                    logger.info(f"Found cache for similar topics for topic {subscribed_topic_id} in region {region_id}.")
                    similar_topics = topic_cache["similar_topics"]
            
            # add up the similarity scores if a topic comes up many times as a similar topic
            for topic_id, score in similar_topics.items():
                if topic_id in similar_topics_scores:
                    similar_topics_scores[topic_id] += score
                else:
                    similar_topics_scores[topic_id] = score

        # remove the subscriptions from the similar topics
        for s in subscriptions_list:
            if s in similar_topics_scores.keys():
                similar_topics_scores.pop(s)
                
        # sort the topics by similarity score
        sorted_topics_list = sorted(similar_topics_scores.items(), key=lambda kv: kv[1], reverse=True)
        if len(sorted_topics_list) > count:
            sorted_topics_list = sorted_topics_list[:count]
        sorted_topics_dict = OrderedDict(sorted_topics_list)

        # output
        if verbose:
            logger.info("Topic scores:")
            logger.info("{:<28} {:<28} {:<18}".format("topic ID", "topic name", "similarity score"))
            for topic_id, score in sorted_topics_dict.items():
                topic_name = self.db1['Topic'].find_one({"_id" : ObjectId(topic_id)})["Name"]
                logger.info("{:<28} {:<28} {:<18}".format(topic_id, topic_name, score))

        # return top few recommended topics
        return list(sorted_topics_dict.keys())
