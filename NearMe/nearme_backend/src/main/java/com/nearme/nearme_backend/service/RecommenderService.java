package com.nearme.nearme_backend.service;

import java.util.List;

import com.nearme.nearme_backend.model.NearMeTopic;

public interface RecommenderService {
    public List<NearMeTopic> getRecommendedTopicList(String regionName, String userId) throws Exception;
}
