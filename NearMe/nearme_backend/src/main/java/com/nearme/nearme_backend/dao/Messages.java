package com.nearme.nearme_backend.dao;

import com.mongodb.*;
import com.mongodb.client.MongoDatabase;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorWriteException;
import com.nearme.nearme_backend.exceptions.MessageNotFoundException;

import org.bson.*;
import org.bson.types.ObjectId;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.bson.conversions.Bson;

import com.mongodb.client.result.DeleteResult;
import static com.mongodb.client.model.Filters.*;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.ConcurrentMap;

public class Messages extends Collection {
    private static final int MAX_ENTRIES = 500;
    Logger logger = LoggerFactory.getLogger(Messages.class);

    protected Messages(MongoDatabase database) {
        collection = database.getCollection("Message");
        
        cache = new LinkedHashMap<String, Document>(MAX_ENTRIES + 1, 1.0f, true) {
            @Override
            protected boolean removeEldestEntry(Map.Entry<String, Document> eldest) {
                return size() > MAX_ENTRIES;
            }
        };
    }

    /**
     * Get geomeshes involved and their count from messages published after timestamp
     * @param timestamp
     * @return List of {"geomesh" : "...", "count": ...}
     */
    public ArrayList<Document> getIncreasedCountPerGeomesh(long timestamp) {
        // This action requires all database entries to participate, not cache simplifiable.
        logger.trace("getIncreasedCountPerGeomesh");
        ArrayList<Document> result = collection.aggregate(
            Arrays.asList(new Document("$match",
                new Document("Timestamp",
                new Document("$gte", timestamp))),
                new Document("$group",
                new Document("_id", "$Geomesh")
                        .append("Count",
                new Document("$sum", 1L)))))
        .into(new ArrayList<>());

        return result;
    }

    public ArrayList<Document> getMessagesAfterTime(long timestamp) {
        logger.trace("getMessagesAfterTime");
        // this too, need all database participation
        ArrayList<Document> result = collection.find(gte("Timestamp", timestamp))
                                               .into(new ArrayList<>());

        return result;
    }

    // coordinates should be in the order of { latitude, longitude, altitude }
    public String addMessage(
        String user,
        double[] coordinates,
        String geomesh,
        String content,
        String preview,
        float[] size,
        String topic,
        boolean anchored,
        long timestamp,
        String imageFormat,
        int color,
        String style,
        float scale,
        float[] rotation
    ) throws DatabaseAccessorWriteException {

        logger.trace("addMessage");
        if (checkID(user) && checkID(topic) && coordinates.length == 3) {
            Document doc = new Document();

            if (anchored) {
                doc.put("Anchored", "true");
            } else {
                doc.put("Anchored", "false");
            }

            BasicDBList c = new BasicDBList();
            c.add(coordinates[0]);
            c.add(coordinates[1]);
            c.add(coordinates[2]);
            doc.put("Location", c);

            BasicDBList s = new BasicDBList();
            if (size != null){
                s.add(size[0]);
                s.add(size[1]);
            }
            doc.put("Size", s);

            doc.put("Color", color);

            BasicDBList rotList = new BasicDBList();
            if (rotation != null){
                rotList.add(rotation[0]);
                rotList.add(rotation[1]);
                rotList.add(rotation[2]);
                rotList.add(rotation[3]);
            }
            doc.put("Rotation", rotList);

            doc.put("Geomesh", geomesh);
            doc.put("Content", content);
            doc.put("Preview", preview);
            doc.put("Topic", topic);
            doc.put("User", user);
            doc.put("Timestamp", timestamp);
            doc.put("Deleted", "false");
            doc.put("Likes", 0);
            doc.put("ImageFormat", imageFormat);
            doc.put("Style", style);
            doc.put("Scale", scale);

            return create(doc);
        }
        return "";
    }

    // This counts... may be simplified by adding field in geomesh collection?
    public long countMessageInGeomesh(String geomesh){
        logger.trace("countMessageInGeomesh");
        return collection.countDocuments(eq("Geomesh", geomesh));
    }

    public long getPrevMessageCountInGeomesh(String geomesh, long timestamp) {
        logger.trace("getPrevMessageCountInGeomesh");
        long result = collection.countDocuments(and(eq("Geomesh", geomesh), lt("Timestamp", timestamp)));
        return result;
    }

    public ArrayList<Document> getNewMessagesInGeomesh(String geomesh, long timestamp) {
        logger.trace("getNewMessagesInGeomesh");
        ArrayList<Document> result = collection.find(and(eq("Geomesh", geomesh), gte("Timestamp", timestamp)))
                                               .into(new ArrayList<>());
        result.forEach(doc -> cache.put(doc.getObjectId("_id").toHexString(), doc));
        return result;
    }

    public ArrayList<Document> getContents(String geomesh, ArrayList<String> topics) throws DatabaseAccessorFindException {
        logger.trace("getContents");
        ArrayList<Document> result = new ArrayList<>();
        try {
           collection.find(
                and(
                    eq("Geomesh", geomesh), 
                    in("Topic", topics), 
                    eq("Anchored", "true"),
                    eq("Deleted", "false")
                ))
                .forEach(doc -> {result.add(doc); cache.put(doc.getObjectId("_id").toHexString(), doc);});
        } catch (MongoException e) {
            throw new DatabaseAccessorFindException("Exception trying to find anchored items in both topics list and geomesh", e);
        }
        return result;
    }

    public ArrayList<Document> getComments(String geomesh, ArrayList<String> topics) throws DatabaseAccessorFindException {
        logger.trace("getComments");
        ArrayList<Document> result = new ArrayList<>();
        try {
            collection.find(
                    and(
                        eq("Geomesh", geomesh), 
                        in("Topic", topics), 
                        eq("Anchored", "false"),
                        eq("Deleted", "false")
                    ))
                    .forEach(doc -> {result.add(doc); cache.put(doc.getObjectId("_id").toHexString(), doc);});
        } catch (MongoException e) {
            throw new DatabaseAccessorFindException("Exception trying to find unanchored items in both topics list and geomesh", e);
        }
        return result;
    }

    // NO REFERENCE FUNCTION
    public ArrayList<Document> getPublications(long timestamp) throws DatabaseAccessorFindException {
        logger.trace("getPublications");
        ArrayList<Document> result = new ArrayList<>();
        try {
            collection.find(gt("Timestamp", timestamp))
                    .forEach(doc -> result.add(doc));
        } catch (MongoException e) {
            throw new DatabaseAccessorFindException("Exception trying to find items based on timestamp", e);
        }
        return result;
    }

    public ArrayList<Document> getCommentsContents() throws DatabaseAccessorFindException {
        logger.trace("getCommentsContents");
        // ??? you find who the hell use this function?
        ArrayList<Document> result = new ArrayList<>();
        try {
            collection.find().forEach(doc -> result.add(doc));
        } catch (MongoException e) {
            throw new DatabaseAccessorFindException("Exception trying to find anything in topics collection", e);
        }
        return result;
    }

    public boolean deleteMessage(String id) throws DatabaseAccessorException, MessageNotFoundException {
        logger.trace("deleteMessage");
        if (checkID(id)) {
            Document doc = read(id);
            if (doc != null) {
                doc.replace("Deleted", "true");
                return update(id, doc);
            }
            return false;
        } else {
            throw new MessageNotFoundException();
        }
    }

    public boolean updateLikes(String id, int count) throws DatabaseAccessorFindException, DatabaseAccessorWriteException, MessageNotFoundException {
        logger.trace("updateLikes");
        if (checkID(id)){
            Document doc = read(id);
            int original_count = doc.getInteger("Likes", 0);
            int new_count = original_count + count;
            if (new_count < 0){
                return false;
            }else{
                return update(id, "Likes", new_count);
            }
        } else {
            throw new MessageNotFoundException();
        }
    }

    public ArrayList<Document> getMessagesInTopic(String topicId, ArrayList<String> meshIds) {
        logger.trace("getMessagesInTopic");
        ArrayList<Document> result = new ArrayList<>();
        collection.find(
                and(
                    in("Geomesh", meshIds), 
                    eq("Topic", topicId),
                    eq("Deleted", "false")
                ))
                .forEach(doc -> {result.add(doc); cache.put(doc.getObjectId("_id").toHexString(), doc);});
        return result;
    }

    public ArrayList<Document> getMessagesInGeomesh(String geomeshId) {
        logger.trace("getMessagesInGeomesh");
        ArrayList<Document> result = new ArrayList<>();
        collection.find(eq("Geomesh", geomeshId))
                .forEach(doc -> {result.add(doc); cache.put(doc.getObjectId("_id").toHexString(), doc);});
        return result;
    }

    public ArrayList<Document> getMessagesInGeomesh(ArrayList<String> meshIds) {
        logger.trace("getMessagesInGeomesh multiple");
        ArrayList<Document> result = new ArrayList<>();
        collection.find(in("Geomesh", meshIds))
                .forEach(doc -> {result.add(doc); cache.put(doc.getObjectId("_id").toHexString(), doc);});
        return result;
    }

    public long deleteByTopic(String topicId) throws MongoException{
        logger.trace("deleteByTopic");
        // local delete
        for (Document doc: cache.values().toArray(new Document[0])) {
            if (topicId.equals(doc.getString("Topic"))) {
                cache.remove(doc.getObjectId("_id").toHexString());
            }
        }
        // remote delete
        Bson query = eq("Topic", topicId);
        DeleteResult result = collection.deleteMany(query);
        return result.getDeletedCount();
    }

}