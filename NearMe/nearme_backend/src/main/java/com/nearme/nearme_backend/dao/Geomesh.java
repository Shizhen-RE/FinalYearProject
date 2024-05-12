package com.nearme.nearme_backend.dao;

import com.mongodb.client.AggregateIterable;
import com.mongodb.client.MongoDatabase;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;

import org.bson.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.Map;


public class Geomesh extends Collection {
    private static final int MAX_ENTRIES = 100;
    Logger logger = LoggerFactory.getLogger(Geomesh.class);
    
    protected Geomesh(MongoDatabase database) {
        collection = database.getCollection("Geomesh");
        cache = new LinkedHashMap<String, Document>(MAX_ENTRIES + 1, 1.0f, true) {
            @Override
            protected boolean removeEldestEntry(Map.Entry<String, Document> eldest) {
                return size() > MAX_ENTRIES;
            }
        };
    }

    public ArrayList<Document> getAllGeomesh() {
        logger.trace("getAllGeomesh");
        ArrayList<Document> result = new ArrayList<>();
        collection.find().forEach(doc -> {
            cache.putIfAbsent(doc.getObjectId("_id").toHexString(), doc);
            result.add(doc);
        });
        return result;
    }

    public Document getGeomesh(String geomeshId) throws DatabaseAccessorException {
        return read(geomeshId);
    }

    /**
     * Gets the geomesh id of a coordinate
     * @param coordinates coordinates of a message
     * @return null if more than 1 ids are found, return _id if successful
     * @throws DatabaseAccessorFindException
     */
    public String getGeomeshByCoord(double[] coordinates) throws DatabaseAccessorFindException {
        logger.trace("getGeomeshByCoord");
        // consider coordinates in Cartesian coordinate system,
        // origin of each geomesh block is the bottom left corner 
        double latitude = coordinates[0];
        double longitude = coordinates[1];

        // try on cache
        for (Document doc: cache.values().toArray(new Document[0])) {
            // toArray is force copying to avoid this being right at the time of splitting
            // geomesh won't be removed, only updated, so this is fine (no fatal error)
            double topLat = doc.getDouble("Latitude");
            double leftLng = doc.getDouble("Longitude");
            double length = doc.getDouble("Length");
            if (latitude < topLat && topLat - latitude < length &&
                longitude > leftLng && longitude - leftLng < length) {
                // Cache hit
                return doc.getObjectId("_id").toHexString();
            }
        }
        
        // mongdb shell syntax exported to Java (1L and other positive int indicates TRUE)
        // ArrayList<Document> result = 
        // collection.aggregate(
        //     Arrays.asList(
        //         new Document("$project", 
        //             new Document(
        //                 "_id", 1L).append(
        //                 "Latitude", 1L).append(
        //                 "Longitude", 1L).append(
        //                 "minLatitude", new Document("$subtract", Arrays.asList("$Latitude", "$Length"))).append(
        //                 "maxLongitude", new Document("$add", Arrays.asList("$Longitude", "$Length")))
        //         ), 
        //         new Document("$match", 
        //             new Document("$and", Arrays.asList(
        //                 new Document("Latitude", new Document("$gte", latitude)), 
        //                 new Document("Longitude", new Document("$lte", longitude)), 
        //                 new Document("minLatitude", new Document("$lt", latitude)), 
        //                 new Document("maxLongitude", new Document("$gt", longitude))))))
        //     ).into(new ArrayList<>());

        Document match = collection.find(
            new Document("$expr", 
                new Document("$and", Arrays.asList(
                    new Document("$gte", Arrays.asList("$Latitude", latitude)), 
                    new Document("$lt", Arrays.asList("$Latitude", 
                        new Document("$sum", Arrays.asList(latitude, "$Length")))), 
                    new Document("$lte", Arrays.asList("$Longitude", longitude)), 
                    new Document("$gt", Arrays.asList("$Longitude", 
                        new Document("$subtract", Arrays.asList(longitude, "$Length")))))))
        ).first();
        
        if (match == null) {
            return "";
        }
        
        // reaching here is cache miss. record it in cache
        String docId = match.getObjectId("_id").toHexString();
        cache.put(docId, match);
        return docId;
    }

    /**
     * Gets the surrounding geomesh id of a coordinate
     * @param currLoc current location coordinates [lat, lon]
     * @param radius radius of the square surrounding area from the center (currLoc) in degree (1 deg ~= 111km)
     * @return list of geomesh id that matches
     */
    public ArrayList<String> getSurroundingGeomesh(double[] currLoc, double radius) {
        logger.trace("getSurroundingGeomesh");
        double userMinLat = currLoc[0] - radius;
        double userMaxLat = currLoc[0] + radius;
        double userMaxLong = currLoc[1] + radius;
        double userMinLong = currLoc[1] - radius;

        // must go to database for completeness
        // AggregateIterable<Document> docs = collection.aggregate(
        //     Arrays.asList(
        //         new Document("$project", 
        //             new Document(
        //                 "_id", 1L).append(
        //                 "Latitude", 1L).append(
        //                 "Longitude", 1L).append(
        //                 "minLatitude", new Document("$subtract", Arrays.asList("$Latitude", "$Length"))).append(
        //                 "maxLongitude", new Document("$add", Arrays.asList("$Longitude", "$Length")))
        //         ), 
        //         new Document("$match", 
        //             new Document("$and", Arrays.asList(
        //                 new Document("minLatitude", new Document("$lte", userMaxLat)),
        //                 new Document("Latitude", new Document("$gte", userMinLat)),
        //                 new Document("Longitude", new Document("$lte", userMaxLong)),
        //                 new Document("maxLongitude", new Document("$gte", userMinLong))
        //             ))
        //         )
        //     )
        // );

        ArrayList<Document> match = collection.find(
            new Document("$expr", 
                new Document("$and", Arrays.asList(
                    new Document("$lte", Arrays.asList(new Document("$subtract", Arrays.asList("$Latitude", "$Length")), userMaxLat)), 
                    new Document("$gte", Arrays.asList("$Latitude", userMinLat)), 
                    new Document("$lte", Arrays.asList("$Longitude", userMaxLong)), 
                    new Document("$gte", Arrays.asList(new Document("$add", Arrays.asList("$Longitude", "$Length")), userMinLong))
                ))
            )
        ).into(new ArrayList<Document>());

        ArrayList<String> result = new ArrayList<>();
        if (match != null) {
            match.forEach(d -> {
                // record in cache
                String id = d.getObjectId("_id").toHexString();
                cache.put(id, d);
                result.add(id);
            });
        }
        return result;
    }

    /**
     * split current geomesh into 4 grids, each with half of the original length
     * @param geomeshId
     * @return a list of 3 newly added geomesh ids
     * @throws DatabaseAccessorException
     */
    public String[] splitGeomesh(String geomeshId) throws DatabaseAccessorException {
        logger.trace("splitGeomesh");
        Document prevGeomesh = read(geomeshId);
        double prevLatitude = prevGeomesh.getDouble("Latitude").doubleValue();
        double prevLongitude = prevGeomesh.getDouble("Longitude").doubleValue();
        double prevLength = prevGeomesh.getDouble("Length").doubleValue();

        double newLength = prevLength / 2;
        // update length of prevGeoMesh
        Document updatedPrevGeomesh = new Document();
        updatedPrevGeomesh.put("Latitude", prevLatitude);
        updatedPrevGeomesh.put("Longitude", prevLongitude);
        updatedPrevGeomesh.put("Length", newLength);
        update(geomeshId, updatedPrevGeomesh);

        // create 3 new geomeshes
        double[] coord1 = { prevLatitude, (prevLongitude + newLength) };
        double[] coord2 = { (prevLatitude - newLength), prevLongitude };
        double[] coord3 = { (prevLatitude - newLength), (prevLongitude + newLength) };

        String[] splittedGeomesh = new String[4];
        splittedGeomesh[0] = geomeshId;
        splittedGeomesh[1] = create(coord1, newLength);
        splittedGeomesh[2] = create(coord2, newLength);
        splittedGeomesh[3] = create(coord3, newLength);

        return splittedGeomesh;
    }

    public double getLength(String geomeshId) throws DatabaseAccessorException {
        logger.trace("getLength");
        Document doc = read(geomeshId);
        return doc.getDouble("Length");
    }

    /**
     * 
     * @param coordinates [latitude, longtitude] is the TOP LEFT corner of the SQUARE geomesh block
     * @param length side length of the block associated with given geomeshID (in degree, roughly 1 deg = 111km)
     * @return empty string if the insert was unsuccessful or the coordinates are not 2 elements
     *         [lat, long], _id otherwise
     * @throws DatabaseAccessorException
     */
    public String create(double[] coordinates, double length) throws DatabaseAccessorException {
        logger.trace("create geomesh");
        // Check that coordinates are [lat, long]
        if (coordinates.length == 2) {
            Document doc = new Document();
            double latitude = coordinates[0];
            double longitude = coordinates[1];

            doc.put("Latitude", latitude);
            doc.put("Longitude", longitude);
            doc.put("Length", length);

            String result = create(doc);
            // Insert into database and check if successful
            if (result.isEmpty()) {
                return "";
            }
            return result;
        }
        return "";
    }
}
