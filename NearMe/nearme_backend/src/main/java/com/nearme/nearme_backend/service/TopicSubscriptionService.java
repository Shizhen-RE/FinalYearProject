package com.nearme.nearme_backend.service;

import java.util.List;

import com.nearme.nearme_backend.model.NearMeTopic;

public interface TopicSubscriptionService {
    public List<NearMeTopic> getTopicList(String regionName, String userId) throws Exception;
    public List<NearMeTopic> getUserTopicList(String userId) throws Exception;
    public List<NearMeTopic> getSubscriptions(String userId) throws Exception;
    public void subscribeTopics(List<String> topicIds, String userId) throws Exception;
    public void unsubscribeTopics(List<String> topicIds, String userId) throws Exception;

    public boolean addTopic(String topicName, String regionName, String userId) throws Exception;
    public boolean deleteTopic(String topicId, String userId) throws Exception;
}
