package com.nearme.nearme_backend.service;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;

import org.json.JSONArray;
import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.bson.Document;

import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.model.ModelHelper;
import com.nearme.nearme_backend.model.NearMeTopic;

public class RecommenderServiceImpl implements RecommenderService {
    DatabaseAccessor da = DatabaseAccessor.getInstance();

    private static final String RECOMMENDATION_URL = "http://recommender-service:9000/getRecommendations/"; // kubernetes deployment
    // private static final String RECOMMENDATION_URL = "http://localhost:9000/getRecommendations/"; // local testing
    Logger logger = LoggerFactory.getLogger(RecommenderServiceImpl.class);

    private int recommend_count = 20;

    @Override
    public List<NearMeTopic> getRecommendedTopicList(String regionName, String userName)
            throws Exception {
        logger.info("getRecommendedTopicList: received request");
        String regionId = da.regions.getID(regionName);
        String userId = da.users.getID(userName);
        List<String> recommendedTopicIds = recommendTopics(regionId, userId);
        logger.debug("getRecommendedTopicList: got recommendation");
        List<NearMeTopic> recommendedTopicList = new ArrayList<NearMeTopic>();
        recommendedTopicIds.forEach(topicId -> {
            try {
                Document topicDoc = da.topics.read(topicId);
                recommendedTopicList.add(ModelHelper.doc2Topic(topicDoc));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
        logger.debug("getRecommendedTopicList: response prepared");
        return recommendedTopicList;
    }

    private List<String> recommendTopics(String regionId, String userId) throws Exception {

        List<String> subscribedTopicIDList = da.users.getSubscriptions(userId);
        int subscriptionCount = subscribedTopicIDList.size();

        // get top popular topics in the region
        List<String> topicIDList = da.topics.getPopularTopics(regionId, subscriptionCount+10);

        // get recommended topics by requesting the recommender endpoint
        logger.trace("requesting recommendation");
        List<String> recommendedIDs = requestRecommendations(regionId, userId);
        logger.trace("request to recommender complete");
        if (recommendedIDs != null){
            topicIDList.addAll(recommendedIDs);
        }
        
        // remove duplicates
        Set<String> set = new HashSet<>(topicIDList);
        topicIDList.clear();
        topicIDList.addAll(set);

        // remove user subscribed topics from the list
        topicIDList.removeAll(subscribedTopicIDList);

        // shuffle to get randomized recommendation preference
        Collections.shuffle(topicIDList);

        // return only 20 topics
        if (topicIDList.size() <= recommend_count){
            return topicIDList;
        }else{
            return topicIDList.subList(0, recommend_count);
        }
    }

    private static List<String> requestRecommendations(String regionId, String userId){
        String targetURL = RECOMMENDATION_URL + "?userId=" + userId + "&regionId=" + regionId;
        
        try {
            // build request
            HttpClient client = HttpClient.newHttpClient();
            HttpRequest request = HttpRequest.newBuilder().uri(URI.create(targetURL)).build();

            // send request
            HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200){
                System.err.println("Request to recommender failed with response code " + response.statusCode());
                return new ArrayList<String>();
            }
            JSONObject resJson = new JSONObject(response.body());
            System.out.println(resJson);

            // get the recommended topic IDs from json response
            ArrayList<String> topicIdList = new ArrayList<String>();
            JSONArray jsonArray = resJson.getJSONArray("recommendations");
            if (jsonArray != null) {
                for (int i=0;i<jsonArray.length();i++){   
                    topicIdList.add((String)jsonArray.get(i));  
                }   
            }  
            
            return topicIdList;

        } catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }
}
