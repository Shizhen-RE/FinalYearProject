package com.nearme.nearme_backend.service;

import java.util.List;

import com.nearme.nearme_backend.model.NearMeARReq;
import com.nearme.nearme_backend.model.NearMeARRes;
import com.nearme.nearme_backend.model.NearMeMessage;

public interface NearMeContentService {
    public void addMessage(NearMeMessage message, String userId, String regionName) throws Exception;
    public void editMessage(NearMeMessage message, String userName) throws Exception;
    public void deleteMessage(String messageId, String userId) throws Exception;
    public List<NearMeMessage> getPublications(int startTime, int count, String userId, boolean getPost) throws Exception;
    public NearMeARRes getMessage(NearMeARReq reqParams, String userId) throws Exception;
    public Boolean addLikes(String userName, String messageId, int count) throws Exception;
    public List<NearMeMessage> getTopicMessages(String topicName, double lat, double lng, String userId) throws Exception;
    public List<NearMeMessage> getNearbyMessages(double lat, double lng, String userName) throws Exception;
}
