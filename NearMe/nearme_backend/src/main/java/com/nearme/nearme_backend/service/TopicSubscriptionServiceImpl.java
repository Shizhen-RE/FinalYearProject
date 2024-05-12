package com.nearme.nearme_backend.service;

import java.util.List;
import java.util.ArrayList;

import org.bson.Document;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.model.ModelHelper;
import com.nearme.nearme_backend.model.NearMeTopic;

public class TopicSubscriptionServiceImpl implements TopicSubscriptionService {
    DatabaseAccessor da = DatabaseAccessor.getInstance();
    Logger logger = LoggerFactory.getLogger(TopicSubscriptionServiceImpl.class);

    @Override
    public List<NearMeTopic> getTopicList(String regionName, String userId) throws Exception {
        logger.info("getTopicList: received request");
        List<String> topicIds = da.regions.getTopicList(da.regions.getID(regionName));
        List<NearMeTopic> topicList = new ArrayList<NearMeTopic>();
        topicIds.forEach(topicId -> {
            try {
                System.out.println(topicId);
                Document topicDoc = da.topics.read(topicId);
                topicList.add(ModelHelper.doc2Topic(topicDoc));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
        logger.debug("getTopicList: response ready");
        return topicList;
    }

 @Override
    public List<NearMeTopic> getSubscriptions(String userName) throws Exception {
        logger.info("getSubscriptions: received request");
        List<String> subIds = da.users.getSubscriptions(da.users.getID(userName));
        List<NearMeTopic> subscriptions = new ArrayList<NearMeTopic>();
        subIds.forEach(topicId -> {
            try {
                Document topicDoc = da.topics.read(topicId);
                subscriptions.add(ModelHelper.doc2Topic(topicDoc));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
        logger.debug("getSubscriptions: response ready");
        return subscriptions;
    }

    @Override
    public void subscribeTopics(List<String> topicIds, String userId) {
        logger.info("subscribeTopics: received request");
        topicIds.forEach(topicId -> {
            try {
                da.users.subscribeTopic(da.users.getID(userId), topicId, da.topics);
                da.topics.incrementSubscriptionCount(topicId);
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
    }

    @Override
    public void unsubscribeTopics(List<String> topicIds, String userId) {
        logger.info("unsubscribeTopics: received request");
        topicIds.forEach(topicId -> {
            try {
                da.users.unsubscribeTopic(da.users.getID(userId), topicId, da.topics);
                da.topics.decrementSubscriptionCount(topicId);
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
    }

    @Override
    public boolean addTopic(String topicName, String regionName, String userName) throws Exception {
        logger.info("addTopic: received request");
        String regionID = da.regions.getID(regionName);
        String userID = da.users.getID(userName);

        // if region don't exist, create a new region
        if (regionID == null){
            regionID = da.regions.create(regionName);
        }

        // create topic
        String topicID = da.topics.create(topicName, regionID, userID);

        // add topic to region
        boolean r = da.regions.addTopic(regionID, topicID, da.topics);

        // add topic to user
        r &= da.users.addTopic(userID, topicID, da.topics);

        return r;
    }

    @Override
    public boolean deleteTopic(String topicId, String userName) throws Exception {
        logger.info("deleteTopic: received request");
        // check if user is authorized to delete
        String userID = da.users.getID(userName);
        String ownerId = (String) da.topics.read(topicId).get("CreatedBy");
        if (!ownerId.equals(userID)) {
            throw new Exception("Not authorized user");
        }

        Document topicDoc = da.topics.read(topicId);
        // delete topic from region
        boolean r = da.regions.deleteTopic((String) topicDoc.get("RegionId"), topicId, da.topics);

        // delete topic from user
        r &= da.users.deleteTopic((String) topicDoc.get("CreatedBy"), topicId, da.topics);

        // delete messages in this topic
        r &= ( da.messages.deleteByTopic(topicId) >= 0);

        // delete topic
        r &= da.topics.deleteTopic(topicId);
        return r;
    }

    @Override
    public List<NearMeTopic> getUserTopicList(String userName) throws Exception {
        logger.info("getUserTopicList: received request");
        String userId = da.users.getID(userName);
        List<String> topicIds = da.users.getTopics(userId);
        List<NearMeTopic> topicList = new ArrayList<NearMeTopic>();
        topicIds.forEach(topicId -> {
            try {
                Document topicDoc = da.topics.read(topicId);
                topicList.add(ModelHelper.doc2Topic(topicDoc));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
        return topicList;
    }
}
