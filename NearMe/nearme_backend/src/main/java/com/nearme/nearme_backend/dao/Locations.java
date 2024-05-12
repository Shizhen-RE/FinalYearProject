package com.nearme.nearme_backend.dao;

import com.mongodb.*;
import com.mongodb.client.MongoDatabase;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorWriteException;

import org.bson.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class Locations extends Collection {
    private static final int MAX_ENTRIES = 100;
    Logger logger = LoggerFactory.getLogger(Locations.class);

    protected Locations(MongoDatabase database) {
        collection = database.getCollection("Location");
        
        cache = new LinkedHashMap<String, Document>(MAX_ENTRIES + 1, 1.0f, true) {
            @Override
            protected boolean removeEldestEntry(Map.Entry<String, Document> eldest) {
                return size() > MAX_ENTRIES;
            }
        };
    }

    public String getID(String address) throws DatabaseAccessorFindException {
        logger.trace("getID location");
        return getID("Address", address);
    }

    /**
     * Gets name of location from its ID
     *
     * @param id The ID of the location
     * @return The name of the location found, or an empty String if no location was
     *         found or the ID was invalid
     */
    public String getAddress(String id) throws DatabaseAccessorFindException {
        logger.trace("getAddress");
        return read(id, "Address");
    }
    
    /**
     * Gets coordinates of location from its ID
     *
     * @param id The ID of the location
     * @return 2-element array of coordinates of the location found, or an 0-element
     *         array if no location was found or the ID was invalid
     */
    public double[] getCoordinates(String id) throws DatabaseAccessorFindException {
        logger.trace("getCoordinates");
        // Make sure that the location is actually in the database
        Document doc = read(id);
        if (doc != null) {
            // Get coordinates and convert to proper return type
            List<Double> c = doc.getList("Coordinates", Double.class);
            double[] coordinates = { c.get(0), c.get(1) };
            return coordinates;
        }
        return new double[0];
    }

    /**
     * Adds location to database if it is not already present
     *
     * @param name        The name of the topic to add
     * @param coordinates The rough coordinates of the location to add
     * @return 1 if location with specified name is already in database, -1 if the
     *         insert was unsuccessful, -2 if the coordinates are not 2 elements
     *         [lat, long], 0 otherwise
     */
    public int create(String address, double[] coordinates) throws DatabaseAccessorFindException, DatabaseAccessorWriteException {
        logger.trace("create location");
        // Check that coordinates are [lat, long]
        if (coordinates.length == 2) {
            // Check if location already exists
            String id = getID(address);
            if (id.isEmpty()) {
                // Populate location document with provided parameters and an empty topic list
                Document doc = new Document();
                doc.put("Address", address);
                BasicDBList c = new BasicDBList();
                c.add(coordinates[0]);
                c.add(coordinates[1]);
                doc.put("Coordinates", c);
                // Insert into database and check if successful
                if (create(doc).isEmpty()) {
                    return -1;
                }
                return 1;
            }
            return 0;
        }
        return -2;
    }

}
