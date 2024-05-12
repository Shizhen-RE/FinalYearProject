package com.nearme.nearme_backend.dao;

import com.mongodb.*;
import com.mongodb.client.MongoDatabase;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorListException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorWriteException;
import com.nearme.nearme_backend.exceptions.UserNotFoundException;

import org.bson.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class Users extends Collection {
    private static final int MAX_ENTRIES = 100;
    Logger logger = LoggerFactory.getLogger(Users.class);

    protected Users(MongoDatabase database) {
        collection = database.getCollection("User");
        cache = new LinkedHashMap<String, Document>(MAX_ENTRIES + 1, 1.0f, true) {
            @Override
            protected boolean removeEldestEntry(Map.Entry<String, Document> eldest) {
                return size() > MAX_ENTRIES;
            }
        };
    }

    public String getID(String name) throws DatabaseAccessorFindException {
        return getID("Name", name);
    }
    
    public String create(String name) throws DatabaseAccessorFindException, DatabaseAccessorWriteException {
        Document doc = new Document();
        doc.put("Name", name);
        doc.put("TotalLikes", 0);
        doc.put("AuthLocations", new BasicDBList());
        doc.put("Subscriptions", new BasicDBList());
        doc.put("Publications", new BasicDBList());
        doc.put("Topics", new BasicDBList());
        doc.put("Liked", new BasicDBList());
        return create(doc, "Name", name);
    }

    public String getName(String id) throws DatabaseAccessorFindException {
        return read(id, "Name");
    }

    /**
     * Adds topic to user subscriptions field if they are not already subscribed to
     * the topic
     *
     * @param userID  The ID of the user
     * @param topicID The ID of the topic to subscribe the user to
     * @param topics The collection that contains the topics
     * @return true if the IDs are both valid, the user and topic both exist, the
     *         user is not already subscribed to the topic, and the insertion has
     *         been sucessful, false otherwise
     */
    public boolean subscribeTopic(String userID, String topicID, Topics topics) throws DatabaseAccessorListException {
        try {
            return addToList(userID, topicID, topics, "Subscriptions");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary userID and secondary topicID", e);
        }
    }

    /**
     * Removes topic from user subscriptions field
     *
     * @param userID  The ID of the user
     * @param topicID The ID of the topic to unsubscribe the user from
     * @param topics The collection that contains the topics
     * @return The name of the topic unsubscribed from if the IDs are both valid,
     *         the user and topic both exist, the user was subscribed to the topic,
     *         and the unsubscription was successful, or an empty String otherwise
     */
    public boolean unsubscribeTopic(String userID, String topicID, Topics topics) throws DatabaseAccessorListException {
        try {
            return removeFromList(userID, topicID, topics, "Subscriptions");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to remove from list with primary userID and secondary topicID", e);
        }
    }
    
    /**
     * Gets the list of topics IDs subscribed to by a particular user
     *
     * @param user The ID of the user
     * @return The list of topic IDs that the user is subscribed to, or if the user
     *         does not exist or the ID is invalid, an empty list
     */
    public List<String> getSubscriptions(String userID) throws DatabaseAccessorFindException {
        return getList(userID, "Subscriptions");
    }

    /**
     * Gets the list of publication IDs made by a particular user
     *
     * @param user The ID of the user
     * @return The list of content IDs that the user published, or if the user does
     *         not exist or the ID is invalid, an empty list
     */
    public List<String> getPublications(String userID) throws DatabaseAccessorFindException {
        return getList(userID, "Publications");
    }

    public List<String> getTopics(String userID) throws DatabaseAccessorFindException {
        return getList(userID, "Topics");
    }

    public boolean addLocation(String userID, String locationID, Locations locations) throws DatabaseAccessorListException {
        try {
            return addToList(userID, locationID, locations, "AuthLocations");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary userID and secondary topicID", e);
        }
    }

    public boolean deleteLocation(String userID, String locationID, Locations locations) throws DatabaseAccessorListException {
        try {
            return removeFromList(userID, locationID, locations, "AuthLocations");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to remove from list with primary userID and secondary topicID", e);
        }
    }

    public int isVerified(String userID, String locationID) throws DatabaseAccessorFindException {
        List<String> userLocs = getList(userID, "AuthLocations");
        for (int i = 0; i < userLocs.size(); i++) {
            if (locationID.equals(userLocs.get(i))) {
                return 1;
            }
        }
        return 0;
    }

    public List<String> getLocations(String userID) throws DatabaseAccessorFindException {
        return getList(userID, "AuthLocations");
    }

    public boolean addPublication(String userID, String messageID, Messages messages) throws DatabaseAccessorListException {
        try {
            return addToList(userID, messageID, messages, "Publications");   
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary userID and secondary messageID", e);
        }
    }

    public boolean deletePublications(String userID, String messageID, Messages messages) throws DatabaseAccessorListException {
        try {
            return removeFromList(userID, messageID, messages, "Publications");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to remove from list with primary userID and secondary messageID", e);
        }
    }

    public boolean addTopic(String userID, String topicID, Topics topics) throws DatabaseAccessorListException {
        try {
            return addToList(userID, topicID, topics, "Topics");   
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary userID and secondary topicID", e);
        }
    }

    public boolean deleteTopic(String userID, String topicID, Topics topics) throws DatabaseAccessorListException {
        try {
            return removeFromList(userID, topicID, topics, "Topics");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to remove from list with primary userID and secondary topicID", e);
        }
    }


    public boolean addToLikes(String userID, String messageID, Messages messages) throws DatabaseAccessorListException{
        try {
            return addToList(userID, messageID, messages, "Liked");
        } catch (DatabaseAccessorFindException | DatabaseAccessorWriteException e) {
            throw new DatabaseAccessorListException("Exception trying to add to list with primary userID and secondary messageID", e);
        }
    }

    public boolean likedMessage(String userID, String messageID) throws DatabaseAccessorFindException, UserNotFoundException {
        /* check if the messageID is in the user's liked list */
        List<String> likedIds = getList(userID, "Liked");
        return likedIds.contains(messageID);
    }

    public boolean incrementTotalLikes(String userID) throws DatabaseAccessorFindException, DatabaseAccessorWriteException {
        int original_count = read(userID).getInteger("TotalLikes", 0);
        return update(userID, "TotalLikes",  original_count + 1);
    }
}
