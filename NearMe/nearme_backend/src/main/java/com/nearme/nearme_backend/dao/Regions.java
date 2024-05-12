package com.nearme.nearme_backend.dao;

import com.mongodb.*;
import com.mongodb.client.MongoDatabase;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorListException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorWriteException;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.bson.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class Regions extends Collection {
    private static final int MAX_ENTRIES = 100;
    Logger logger = LoggerFactory.getLogger(Regions.class);

    protected Regions(MongoDatabase database) {
        collection = database.getCollection("Region");
        
        cache = new LinkedHashMap<String, Document>(MAX_ENTRIES + 1, 1.0f, true) {
            @Override
            protected boolean removeEldestEntry(Map.Entry<String, Document> eldest) {
                return size() > MAX_ENTRIES;
            }
        };
    }

    public String getID(String name) throws DatabaseAccessorFindException {
        logger.trace("getID regions");
        return getID("Name", name);
    }

    public String getName(String id) throws DatabaseAccessorFindException {
        logger.trace("getName region");
        return read(id, "Name");
    }

    public String create(String name) throws DatabaseAccessorFindException, DatabaseAccessorWriteException {
        logger.trace("create region");
        Document doc = new Document();
        doc.put("Name", name);
        doc.put("Topics", new BasicDBList());
        return create(doc, "Name", name);
    }

    /**
     * Associates topic with region if the topic is not already associated with
     * that region
     *
     * @param topic    The ID of the topic
     * @param location The ID of the region to associate the topic with
     * @return true if the IDs are both valid, the topic and region both exist,
     *         the topic is not already associated with the location, and the
     *         insertion has been sucessful, false otherwise
     */
    public boolean addTopic(String regionID, String topicID, Topics topics) throws DatabaseAccessorListException {
        logger.trace("addTopic");
        try {
            return addToList(regionID, topicID, topics, "Topics");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary regionID and secondary topicID", e);
        }
    }

    public boolean deleteTopic(String regionID, String topicID, Topics topics) throws DatabaseAccessorListException {
        logger.trace("deleteTopic");
        try {
            return removeFromList(regionID, topicID, topics, "Topics");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary regionID and secondary topicID", e);
        }
    }
    
    public List<String> getTopicList(String regionID) throws DatabaseAccessorFindException {
        logger.trace("getTopicList");
        return getList(regionID, "Topics");
    }

}
