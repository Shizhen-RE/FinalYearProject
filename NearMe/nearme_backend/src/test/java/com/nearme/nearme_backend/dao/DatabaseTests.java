package com.nearme.nearme_backend.dao;

import static org.hamcrest.CoreMatchers.*;
import static org.junit.Assert.*;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;

import org.bson.Document;
import org.junit.Test;

import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorFindException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorListException;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorWriteException;

public class DatabaseTests {

    private DatabaseAccessor accessor = DatabaseAccessor.getInstance();
    private String[] name = { "FOO", "BAR", "BUZ" };
    private String region = "testregion";
    private String idLong = "1234567890abcdef";
    private String idNonHex = "This ID is an invalid ID";
    private String idUnused = "1234567890abcdef01234567";

    @Test
    public void testTopics() throws DatabaseAccessorException {

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.topics.delete(accessor.topics.getID(name[i]));
        }

        // Make sure that the test entry does not exist (if this fails, the above
        // deletion may have failed)
        assertThat(accessor.topics.getID(name[0]), equalTo(""));

        // Make sure that invalid length IDs do not return results
        assertThat(accessor.topics.getName(idLong), equalTo(""));
        assertThat(accessor.topics.delete(idLong), equalTo(null));

        // Make sure that non-hexidecimal IDs do not return results
        assertThat(accessor.topics.getName(idNonHex), equalTo(""));
        assertThat(accessor.topics.delete(idNonHex), equalTo(null));

        // Make sure that valid unused IDs do not return results
        assertThat(accessor.topics.getName(idUnused), equalTo(""));
        assertThat(accessor.topics.delete(idUnused), equalTo(null));

        // Add test entry to database
        assertThat(accessor.topics.create(name[0], region, name[0]), not(equalTo("")));

        // Ensure that inserting duplicate test entry fails
        assertThat(accessor.topics.create(name[0], region, name[0]), equalTo(""));

        // Make sure that both get functions work
        String id = accessor.topics.getID(name[0]);
        ArrayList<String> list = new ArrayList<>();
        list.add(id);
        assertThat(accessor.topics.getName(id), equalTo(name[0]));
        List<String> topicList = accessor.topics.getAllTopics();
        boolean positive = false;
        boolean negative = true;
        for (int i = 0; i < topicList.size(); i++) {
            String t = accessor.topics.getName(topicList.get(i));
            positive = (positive || t.equals(name[0]));
            negative = (negative && !(t.equals(name[1]) || t.equals(name[1])));
        }
        assertThat(positive && negative, equalTo(true));

        // Delete the test entry
        assertThat(accessor.topics.delete(id).get("Name"), equalTo(name[0]));

        // Make sure deleting non-existant entries does not work
        assertThat(accessor.topics.delete(id), equalTo(null));

        // Make sure that the test entry does not exist
        assertThat(accessor.topics.getID(name[0]), equalTo(""));
    }

    @Test
    public void testLocations() throws DatabaseAccessorException {

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.locations.delete(accessor.locations.getID(name[i]));
        }

        // Make sure that the test entry does not exist (if this fails, the above
        // deletion may have failed)
        assertThat(accessor.locations.getID(name[0]), equalTo(""));

        // Make sure that invalid length IDs do not return results
        // assertThat(accessor.topics.getName(idLong), equalTo(""));

        // Make sure that non-hexidecimal IDs do not return results
        // assertThat(accessor.topics.getName(idNonHex), equalTo(""));

        // Make sure that valid unused IDs do not return results
        // assertThat(accessor.topics.getName(idUnused), equalTo(""));

        // Test creating locations
        // Make sure that bad coordinate formats fail
        double[] tooShort = { 1.2f };
        double[] tooLong = { 1.2f, 3.4f, 5.6f };
        assertThat(accessor.locations.create(name[0], tooShort), equalTo(-2));
        assertThat(accessor.locations.create(name[0], tooLong), equalTo(-2));
        // Verify success
        double[] coordinates = { 1.2f, 3.4f };
        assertThat(accessor.locations.create(name[0], coordinates), equalTo(1));
        // Make sure that duplicate names fail
        assertThat(accessor.locations.create(name[0], coordinates), equalTo(0));
        double[] alternateCoordinates = { 3.4f, 5.6f };
        assertThat(accessor.locations.create(name[0], alternateCoordinates), equalTo(0));
        // Note: the case for return -1 cannot be invoked artificially

        // Make sure that basic get functions work
        String id = accessor.locations.getID(name[0]);
        assertThat(accessor.locations.getAddress(id), equalTo(name[0]));
        assertThat(accessor.locations.getCoordinates(id), equalTo(coordinates));

        // Delete the test entry
        assertThat(accessor.locations.delete(id).get("Address"), equalTo(name[0]));

        // Make sure deleting non-existant entries does not work
        assertThat(accessor.locations.delete(id), equalTo(null));

        // Make sure that the test entry does not exist
        assertThat(accessor.locations.getID(name[0]), equalTo(""));
    }

    @Test
    public void testRegions() throws DatabaseAccessorException {

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.regions.delete(accessor.regions.getID(name[i]));
        }

        // Make sure that the test entry does not exist (if this fails, the above
        // deletion may have failed)
        assertThat(accessor.regions.getID(name[0]), equalTo(""));

        // Make sure that invalid length IDs do not return results
        // assertThat(accessor.topics.getName(idLong), equalTo(""));

        // Make sure that non-hexidecimal IDs do not return results
        // assertThat(accessor.topics.getName(idNonHex), equalTo(""));

        // Make sure that valid unused IDs do not return results
        // assertThat(accessor.topics.getName(idUnused), equalTo(""));

        // Test creating locations
        // Verify success
        assertThat(accessor.regions.create(name[0]), not(equalTo("")));
        // Make sure that duplicate names fail
        assertThat(accessor.regions.create(name[0]), equalTo(""));

        // Make sure that basic get functions work
        String id = accessor.regions.getID(name[0]);
        assertThat(accessor.regions.getName(id), equalTo(name[0]));

        // Delete the test entry
        assertThat(accessor.regions.delete(id).get("Name"), equalTo(name[0]));

        // Make sure deleting non-existant entries does not work
        assertThat(accessor.regions.delete(id), equalTo(null));

        // Make sure that the test entry does not exist
        assertThat(accessor.regions.getID(name[0]), equalTo(""));
    }

    @Test
    public void testTopicsRegions() throws DatabaseAccessorException {

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.topics.delete(accessor.topics.getID(name[i]));
            accessor.regions.delete(accessor.regions.getID(name[i]));
        }

        // Create test region
        assertThat(accessor.regions.create(name[0]), not(equalTo("")));
        String regionID = accessor.regions.getID(name[0]);

        // Make sure region has no topics associated with it yet
        ArrayList<String> empList = new ArrayList<>();
        assertThat(accessor.regions.getTopicList(regionID), equalTo(empList));

        // Create test topics
        // First topic will be added immediately with region
        // Second topic will be added later (for cases where locations are created after
        // topics they should be associated with?)
        String[] id = { accessor.topics.create(name[0], region, name[0]), accessor.topics.create(name[1], region, name[1]) };
        accessor.regions.addTopic(regionID, id[0], accessor.topics);
        assertThat(id[0], not(equalTo("")));
        assertThat(id[1], not(equalTo("")));

        // Add second topic to region
        // Make sure that invalid length IDs do not return results
        // assertThat(accessor.topics.getName(idLong), equalTo(""));
        // Make sure that non-hexidecimal IDs do not return results
        // assertThat(accessor.topics.getName(idNonHex), equalTo(""));
        // Make sure that valid unused IDs do not return results
        // assertThat(accessor.topics.getName(idUnused), equalTo(""));
        // Verify success
        assertThat(accessor.regions.addTopic(regionID, id[1], accessor.topics), equalTo(true));
        // Make sure that duplicates fail
        assertThat(accessor.regions.addTopic(regionID, id[0], accessor.topics), equalTo(false));
        assertThat(accessor.regions.addTopic(regionID, id[1], accessor.topics), equalTo(false));

        // Test that the locational topic list works correctly
        List<String> list = accessor.regions.getTopicList(regionID);
        for (int i = 0; i < list.size(); i++) {
            assertThat(list.get(i), equalTo(id[i]));
            assertThat(accessor.topics.getName(list.get(i)), equalTo(name[i]));
        }

        // Delete the test entries
        assertThat(accessor.topics.delete(id[0]).get("Name"), equalTo(name[0]));
        assertThat(accessor.topics.delete(id[1]).get("Name"), equalTo(name[1]));
        assertThat(accessor.regions.delete(regionID).get("Name"), equalTo(name[0]));
    }

    @Test
    public void testUsers() throws DatabaseAccessorException {

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.users.delete(accessor.users.getID(name[i]));
        }

        // Make sure that the test entry does not exist (if this fails, the above
        // deletion may have failed)
        assertThat(accessor.users.getID(name[0]), equalTo(""));

        // Make sure that invalid length IDs do not return results
        assertThat(accessor.users.getName(idLong), equalTo(""));
        assertThat(accessor.users.delete(idLong), equalTo(null));

        // Make sure that non-hexidecimal IDs do not return results
        assertThat(accessor.users.getName(idNonHex), equalTo(""));
        assertThat(accessor.users.delete(idNonHex), equalTo(null));

        // Make sure that valid unused IDs do not return results
        assertThat(accessor.users.getName(idUnused), equalTo(""));
        assertThat(accessor.users.delete(idUnused), equalTo(null));

        // Add test entry to database
        assertThat(accessor.users.create(name[0]), not(equalTo("")));

        // Ensure that inserting duplicate test entry fails
        assertThat(accessor.users.create(name[0]), equalTo(""));

        // Make sure that both get functions work
        String id = accessor.users.getID(name[0]);
        assertThat(accessor.users.getName(id), equalTo(name[0]));

        // Delete the test entry
        assertThat(accessor.users.delete(id).get("Name"), equalTo(name[0]));

        // Make sure deleting non-existant entries does not work
        assertThat(accessor.users.delete(id), equalTo(null));

        // Make sure that the test entry does not exist
        assertThat(accessor.users.getID(name[0]), equalTo(""));
    }

    @Test
    public void testUsersTopics() throws DatabaseAccessorException {

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.users.delete(accessor.users.getID(name[i]));
            accessor.topics.delete(accessor.topics.getID(name[i]));
        }

        // Create test user
        String userID = accessor.users.create(name[0]);
        assertThat(userID, not(equalTo("")));

        // Make sure user has no has no subscriptions yet
        ArrayList<String> id = new ArrayList<>();
        assertThat(accessor.users.getSubscriptions(userID), equalTo(id));

        // Create test topic
        id.add(accessor.topics.create(name[0], region, userID));
        assertThat(id.get(0), not(equalTo("")));

        // Subscribe user to the topic
        // Make sure that invalid length IDs do not return results
        // assertThat(accessor.topics.getName(idLong), equalTo(""));
        // Make sure that non-hexidecimal IDs do not return results
        // assertThat(accessor.topics.getName(idNonHex), equalTo(""));
        // Make sure that valid unused IDs do not return results
        // assertThat(accessor.topics.getName(idUnused), equalTo(""));
        // Verify success
        assertThat(accessor.users.subscribeTopic(userID, id.get(0), accessor.topics), equalTo(true));
        assertThat(accessor.users.getSubscriptions(userID), equalTo(id));

        // Create a second test topic and subscribe the user to it
        id.add(accessor.topics.create(name[1], region, userID));
        assertThat(id.get(1), not(equalTo("")));
        assertThat(accessor.users.subscribeTopic(userID, id.get(1), accessor.topics), equalTo(true));
        assertThat(accessor.users.getSubscriptions(userID), equalTo(id));

        // Make sure that duplicates fail
        assertThat(accessor.users.subscribeTopic(userID, id.get(0), accessor.topics), equalTo(false));
        assertThat(accessor.users.subscribeTopic(userID, id.get(1), accessor.topics), equalTo(false));

        // Unsubscribe user from topics
        ArrayList<String> un = new ArrayList<>();
        assertThat(accessor.users.unsubscribeTopic(userID, id.get(0), accessor.topics), equalTo(true));
        un.add(id.get(0));
        id.remove(0);
        assertThat(accessor.users.getSubscriptions(userID), equalTo(id));
        assertThat(accessor.users.unsubscribeTopic(userID, id.get(0), accessor.topics), equalTo(true));
        un.add(id.get(0));
        id.remove(0);
        assertThat(accessor.users.getSubscriptions(userID), equalTo(id));

        // Make sure redundant unsubs fail
        assertThat(accessor.users.unsubscribeTopic(userID, un.get(0), accessor.topics), equalTo(false));
        assertThat(accessor.users.unsubscribeTopic(userID, un.get(1), accessor.topics), equalTo(false));

        // Delete the test entries
        assertThat(accessor.topics.delete(un.get(0)).get("Name"), equalTo(name[0]));
        assertThat(accessor.topics.delete(un.get(1)).get("Name"), equalTo(name[1]));
        assertThat(accessor.users.delete(userID).get("Name"), equalTo(name[0]));
    }

    @Test
    public void testUsersLocations() throws DatabaseAccessorException { // MUST BE CHANGED WHEN LOCATION OWNERSHIP DESIGN CHANGES

        // Delete test entries in case of previous failed tests
        for (int i = 0; i < name.length; i++) {
            accessor.users.delete(accessor.users.getID(name[i]));
            accessor.locations.delete(accessor.locations.getID(name[i]));
        }

        // Create test user
        String userID = accessor.users.create(name[0]);
        assertThat(userID, not(equalTo("")));

        // Make sure user has no has no authorized locations yet
        ArrayList<String> id = new ArrayList<>();
        assertThat(accessor.users.getLocations(userID), equalTo(id));

        // Create test location
        double[] coordinates = { 1.2f, 3.4f };
        assertThat(accessor.locations.create(name[0], coordinates), equalTo(1));
        id.add(accessor.locations.getID(name[0]));
        assertThat(accessor.users.isVerified(userID, id.get(0)), equalTo(0));

        // Authorize user for that location
        // Make sure that invalid length IDs do not return results
        // assertThat(accessor.topics.getName(idLong), equalTo(""));
        // Make sure that non-hexidecimal IDs do not return results
        // assertThat(accessor.topics.getName(idNonHex), equalTo(""));
        // Make sure that valid unused IDs do not return results
        // assertThat(accessor.topics.getName(idUnused), equalTo(""));
        // Verify success
        assertThat(accessor.users.addLocation(userID, id.get(0), accessor.locations), equalTo(true));

        // Check verified status
        assertThat(accessor.users.getLocations(userID), equalTo(id));
        assertThat(accessor.users.isVerified(userID, id.get(0)), equalTo(1));

        // Create a second test location and check verified status
        assertThat(accessor.locations.create(name[1], coordinates), equalTo(1));
        id.add(accessor.locations.getID(name[1]));
        assertThat(accessor.users.isVerified(userID, id.get(0)), equalTo(1));
        assertThat(accessor.users.isVerified(userID, id.get(1)), equalTo(0));

        // Verify the user and check verified status
        assertThat(accessor.users.addLocation(userID, id.get(1), accessor.locations), equalTo(true));
        assertThat(accessor.users.getLocations(userID), equalTo(id));
        assertThat(accessor.users.isVerified(userID, id.get(0)), equalTo(1));
        assertThat(accessor.users.isVerified(userID, id.get(1)), equalTo(1));

        // Make sure that duplicates fail
        assertThat(accessor.users.addLocation(userID, id.get(0), accessor.locations), equalTo(false));
        assertThat(accessor.users.addLocation(userID, id.get(1), accessor.locations), equalTo(false));

        // Unverify user for location
        ArrayList<String> un = new ArrayList<>();
        assertThat(accessor.users.deleteLocation(userID, id.get(0), accessor.locations), equalTo(true));
        un.add(id.get(0));
        id.remove(0);
        assertThat(accessor.users.getLocations(userID), equalTo(id));
        assertThat(accessor.users.deleteLocation(userID, id.get(0), accessor.locations), equalTo(true));
        un.add(id.get(0));
        id.remove(0);
        assertThat(accessor.users.getLocations(userID), equalTo(id));

        // Make sure redundant deauthorizations fail
        assertThat(accessor.users.deleteLocation(userID, un.get(0), accessor.locations), equalTo(false));
        assertThat(accessor.users.deleteLocation(userID, un.get(1), accessor.locations), equalTo(false));

        // Delete the test entries
        assertThat(accessor.locations.delete(un.get(0)).get("Address"), equalTo(name[0]));
        assertThat(accessor.locations.delete(un.get(1)).get("Address"), equalTo(name[1]));
        assertThat(accessor.users.delete(userID).get("Name"), equalTo(name[0]));
    }

    // @Test
    public void testContents() throws DatabaseAccessorException {
        // Create accessor instance if it does not already exist
        DatabaseAccessor accessor = DatabaseAccessor.getInstance();

        // Delete all content
        accessor.messages.getCommentsContents().forEach(doc -> {
            try {
                accessor.messages.delete(doc.get("_id").toString());
            } catch (DatabaseAccessorException e) {
            }
        });

        // Create test user
        String userID = accessor.users.create(name[0]);
        assertThat(userID, not(equalTo("")));

        // Create test region
        String regionID = accessor.regions.create(name[0]);
        assertThat(regionID, not(equalTo("")));

        // Create test topics
        ArrayList<String> topicID = new ArrayList<>();
        topicID.add(accessor.topics.create(name[0], region, userID));
        topicID.add(accessor.topics.create(name[1], region, userID));
        accessor.regions.addTopic(regionID, topicID.get(0), accessor.topics);
        accessor.regions.addTopic(regionID, topicID.get(1), accessor.topics);
        assertThat(topicID, not(equalTo("")));

        // Create some sizes
        float[] size = { 10, 10 };
        float[] broken = { 10, 10, 10 };

        // Have the user publish some content at that location
        // Make sure that invalid length IDs do not return results
        // assertThat(accessor.topics.getName(idLong), equalTo(""));
        // Make sure that non-hexidecimal IDs do not return results
        // assertThat(accessor.topics.getName(idNonHex), equalTo(""));
        // Make sure that valid unused IDs do not return results
        // assertThat(accessor.topics.getName(idUnused), equalTo(""));
        // Verify success
        ArrayList<String> messID = new ArrayList<>();
        double[] coordinates = { 1.2f, 3.4f, 5.6f };
        Document d = new Document();
        messID.add(accessor.messages.create(d));//userID, coordinates, 0, name[0], "Lorem Ipsum", size, topicID.get(0), "text", true, Instant.now().getEpochSecond()));
        //messID.add(accessor.messages.create(userID, coordinates, 0, name[1], "Lorem Ipsum",  size, topicID.get(1), "text", false, Instant.now().getEpochSecond()));
        //messID.add(accessor.messages.create(userID, coordinates, 0, name[2], "Lorem Ipsum", size, topicID.get(1), "image", true, Instant.now().getEpochSecond()));
        messID.forEach(m -> {
            assertThat(m, not(equalTo("")));
            try {
                assertThat(accessor.users.addPublication(userID, m, accessor.messages), equalTo(true));
            } catch (DatabaseAccessorException e) {
            }
        });

        // Check to make sure that the publications are associated with the user
        List<String> pubs = accessor.users.getPublications(userID);
        messID.forEach(m -> {
            assertThat(pubs.contains(m), equalTo(true));
        });

        messID.forEach(m -> {
            try {
                accessor.messages.delete(m);
            } catch (DatabaseAccessorException e) {
            }
        });
        accessor.users.delete(userID);
        accessor.regions.delete(regionID);
        topicID.forEach(t -> {
            try {
                accessor.topics.delete(t);
            } catch (DatabaseAccessorException e) {
            }
        });
    }

    @Test
    public void testGeomesh() throws DatabaseAccessorException {

        // Delete all geomesh
        accessor.geomesh.getAllGeomesh().forEach(doc -> {
            try {
                accessor.geomesh.delete(doc.get("_id").toString());
            } catch (DatabaseAccessorFindException e) {
                // TODO Auto-generated catch block
                e.printStackTrace();
                fail("failed to delete all geomesh");
            }
        });

        // create Geomesh
        double[] coordinates = { -180f, -90f };
        try {
            String geomeshID = accessor.geomesh.create(coordinates, (double)1.0);
            assertThat(geomeshID, not(equalTo("")));

            ArrayList<Document> result = accessor.geomesh.getAllGeomesh();
            assertTrue(result.size() == 1);
    
            assertThat(accessor.geomesh.getLength(geomeshID), equalTo(1f));

        } catch (DatabaseAccessorWriteException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
            fail("failed to create geomesh");
        }
    }

}