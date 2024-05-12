package com.nearme.nearme_backend.service;

import java.util.ArrayList;

import org.bson.Document;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.springframework.beans.factory.annotation.Autowired;

import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorWriteException;

import static org.hamcrest.CoreMatchers.*;
import static org.junit.Assert.*;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class GeomeshTest {
    @Autowired
    GeomeshServiceImpl geomeshServiceImpl;

    private static DatabaseAccessor accessor;
    private String[] name = { "FOO", "BAR", "BUZ" };
    private String region = "testregion";
    private String userID;
    private String regionID;
    private String topicID;
    private long messageCreationTime;
    private ArrayList<String> messageID = new ArrayList<String>();
    private ArrayList<String> geomeshID = new ArrayList<String>();

    @Before
    public void setUp() throws DatabaseAccessorException {
        // Create accessor instance if it does not already exist
        accessor = DatabaseAccessor.getInstance();

        // Delete all geomesh
        accessor.geomesh.getAllGeomesh().forEach(doc -> {
            try {
                accessor.geomesh.delete(doc.get("_id").toString());
            } catch (DatabaseAccessorFindException e) {
                // TODO Auto-generated catch block
                e.printStackTrace();
            }
        });
        
        // create a sample 3x3 grid geomesh, each with 1 degree length
        // lat ∈ [-90, 87] and lon ∈ [-180, 177]
        for (double i = 0; i < 3; i ++) {
            for (double j = 0; j < 3; j ++){
                double[] coordinates = { 90f-i, -180f+j };
                geomeshID.add(accessor.geomesh.create(coordinates, 1f));
                assertThat(geomeshID, not(equalTo("")));
            }
        }
        // create two bigger geomesh with 2 degree length
        double[] coord1 = { (double)(90.0), (double)(-177.0) };
        geomeshID.add(accessor.geomesh.create(coord1, (double)(2)));
        assertThat(geomeshID, not(equalTo("")));
        double[] coord2 = { (double)(87.0), (double)(-180.0) };
        geomeshID.add(accessor.geomesh.create(coord2, (double)(2)));
        assertThat(geomeshID, not(equalTo("")));


        // Create test user
        userID = accessor.users.create(name[0]);
        assertThat(userID, not(equalTo("")));

        // Create test region
        regionID = accessor.regions.create(name[0]);
        assertThat(regionID, not(equalTo("")));

        // Create test topics
        topicID = accessor.topics.create(name[0], region, userID);
        assertThat(topicID, not(equalTo("")));

        messageCreationTime = System.currentTimeMillis();           
    }

    @After
    public void cleanUp() throws DatabaseAccessorFindException {
        // delete all messages created after messageCreationTime  
        if (!messageID.isEmpty()) {
            messageID.forEach(m -> {
                try {
                    accessor.messages.delete(m);
                } catch (DatabaseAccessorFindException e) {
                    // TODO Auto-generated catch block
                    e.printStackTrace();
                }
            });
            messageID.clear();
        }

        // remove all geomesh
        accessor.geomesh.getAllGeomesh().forEach(g -> { 
            try {
                accessor.geomesh.delete(g.get("_id").toString());
            } catch (DatabaseAccessorFindException e) {
                // TODO Auto-generated catch block
                e.printStackTrace();
            } 
        });
        accessor.topics.delete(topicID);
        accessor.regions.delete(regionID);
        accessor.users.delete(userID);

    }

    @Test
    public void testGeomeshSplit() throws DatabaseAccessorException{
        accessor.geomesh.splitGeomesh(geomeshID.get(0));
        
        // check if one of the grid is split into 4
        ArrayList<Document> result = accessor.geomesh.getAllGeomesh();
        assertEquals(14, result.size());
    }

    @Test
    public void testGeomeshByCoord() throws DatabaseAccessorFindException {
        double[] coord = { 89.34f, -179.56f };
        String result = accessor.geomesh.getGeomeshByCoord(coord);
        assertEquals(result, geomeshID.get(0));
    }

    @Test
    public void testGetSurroundingGeomesh() {
        double[] userLocation = { 88.5f, -178.5f }; // [lat, lon]
        ArrayList<String> result = accessor.geomesh.getSurroundingGeomesh( userLocation, 1);
        assertThat(result.size(), equalTo(9));
        assertTrue(result.contains(geomeshID.get(0)));
        assertTrue(result.contains(geomeshID.get(1)));
        assertTrue(result.contains(geomeshID.get(2)));
        assertTrue(result.contains(geomeshID.get(3)));
        assertTrue(result.contains(geomeshID.get(4)));
        assertTrue(result.contains(geomeshID.get(5)));
        assertTrue(result.contains(geomeshID.get(6)));
        assertTrue(result.contains(geomeshID.get(7)));
        assertTrue(result.contains(geomeshID.get(8)));
    }

    /*
     * Scheduled task runs every 4 hours and is already manually tested
    @Test
    public void testScheduledGeomeshUpdate() throws Exception {
        // Create 200 messages in the same geomesh range
        for (int i = 0; i < 110; i ++) {
            Random rand = new Random();
            float randLat = rand.nextFloat() * (90 - 89) + 89;
            float randLon = rand.nextFloat() * (-179 + 180) - 180;
            float[] coords = { randLat, randLon, 0f };
            float[] size = {0f, 0f};
            messageID.add(accessor.messages.addMessage(userID, coords, "", "hello", "", size, topicID, "text", false, System.currentTimeMillis()));
        }

        ArrayList<Document> newMessages = accessor.messages.getMessagesAfterTime(messageCreationTime);
        assertTrue(newMessages.size() == messageID.size());

        Thread.sleep(90000L);
        
        // check if one of the grid is split into 4
        ArrayList<Document> result = accessor.geomesh.getAllGeomesh();
        assertTrue(result.size() == 4);
        // check if the geomesh entry of these messages are still empty
        newMessages = accessor.messages.getMessagesAfterTime(messageCreationTime);
        assertNotEquals(newMessages.get(10).getString("Geomesh"), "");
    }
    */
    
}
