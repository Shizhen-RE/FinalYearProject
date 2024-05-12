package com.nearme.nearme_backend.service;

import java.time.Instant;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.Arrays;
import java.util.List;
import java.util.Set;

import org.bson.Document;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.exceptions.TopicNotFoundException;
import com.nearme.nearme_backend.exceptions.UserNotFoundException;
import com.nearme.nearme_backend.model.ModelHelper;
import com.nearme.nearme_backend.model.NearMeARReq;
import com.nearme.nearme_backend.model.NearMeARRes;
import com.nearme.nearme_backend.model.NearMeMessage;

public class NearMeContentServiceImpl implements NearMeContentService {
    DatabaseAccessor da = DatabaseAccessor.getInstance();
    // TODO: change this hardcoded value to a request parameter (user setting)
    // NOTE: then this param will go into NearMeARReq class
    final static double SURROUNDING_RADIUS = 0.005; // 0.01 degree in geo coordinate ~= 1.11km, 0.005 degree ~= 555m
    final static byte[] EMPTY_BYTE_ARRAY = new byte[]{};

    Logger logger = LoggerFactory.getLogger(NearMeContentServiceImpl.class);

    @Override
    public void addMessage(NearMeMessage message, String userId, String regionName) throws Exception {
        logger.info("addMessage: received request");
        String topicId = message.getTopic();
        // validate topic exists
        if(da.topics.getName(topicId).isEmpty()){
            throw new TopicNotFoundException();
        }
        // validate user exist
        if (da.users.getID(userId).isEmpty()) {
            throw new UserNotFoundException();
        }

        logger.debug("addMessage: check pass");

        // update messageCount in topic
        da.topics.incrementMessageCount(topicId);

        String geomesh = da.geomesh.getGeomeshByCoord(message.getCoordinates());
        String messageId = da.messages.addMessage(
            da.users.getID(userId),
            message.getCoordinates(),
            geomesh,
            message.getContent(),
            message.getPreview(),
            message.getSize(),
            message.getTopic(),
            message.isIsAR(),
            message.getTimestamp(),
            message.getImageFormat(),
            message.getColor(),
            message.getStyle(),
            message.getScale(),
            message.getRotation()
        );

        // update user record
        da.users.addPublication(da.users.getID(userId), messageId, da.messages);
        logger.debug("addMessage: all DB action done");
    }

    @Override
    public void editMessage(NearMeMessage message, String userName) throws Exception {
        logger.info("editMessage: received request");
        // find the whose message it it
        Document dbMessage = da.messages.read(message.getID());
        if (!da.users.getName(dbMessage.getString("User")).equals(userName)) {
            throw new Exception("Not authorized user");
        }
        // change the message document topic / content / timestamp
        da.messages.update(message.getID(), "Topic", message.getTopic());
        da.messages.update(message.getID(), "Content", message.getContent());
        da.messages.update(message.getID(), "Timestamp", message.getTimestamp());
        // note only them are allowed to change by frontend
        logger.debug("editMessage: changes done on message doc");
        // go to the user and rearrange list of publications
        Document owner = da.users.read(message.getUser());
        da.users.deletePublications(dbMessage.getString("User"), message.getID(), da.messages);
        da.users.addPublication(dbMessage.getString("User"), message.getID(), da.messages);
        List<String> ownerPubs = owner.getList("Publications", String.class);
        // WARNING condition that has consequences
        if (ownerPubs.remove(message.getID()) && ownerPubs.add(message.getID())) {
            owner.replace("Publications", ownerPubs);
            da.users.update(message.getUser(), owner);
            logger.debug("editMessage: changes done on user doc");
            return;
        } else {
            throw new Exception("Weird... cannot change the user table pub list");
        }
    }

    @Override
    public void deleteMessage(String messageId, String userName) throws Exception {
        logger.info("deleteMessage: received request");
        // validity check
        String ownerId = (String) da.messages.read(messageId).get("User");
        String userId = da.users.getID(userName);
        if (!ownerId.equals(userId)) {
            throw new Exception("Not authorized user");
        }
        String topicId = (String) da.messages.read(messageId).get("Topic");
        da.messages.deleteMessage(messageId);
        da.users.deletePublications(userId, messageId, da.messages);
        da.topics.decrementMessageCount(topicId);
        logger.debug("deleteMessage: all DB action done");
    }

    @Override
    public List<NearMeMessage> getPublications(int startTime, int count, String userName, boolean getPost) throws Exception {
        logger.info("getPublications (history): received request");
        String userId = da.users.getID(userName);
        List<String> userPubIds = da.users.getPublications(userId);
        List<NearMeMessage> userPubs = new ArrayList<NearMeMessage>();
        // redo this portion, we need to cut looping in the middle.
        int fetchCount = 0;
        int fetchLoc = userPubIds.size() - 1;
        while (fetchLoc >= 0 && fetchCount < count) {
            Document publication = da.messages.read(userPubIds.get(fetchLoc));
            fetchLoc--;
            // two checks: timestamp is after (before) startTime; check the type of publication
            if ((long) publication.get("Timestamp") > (long) startTime ||
                (((String) publication.get("Anchored")).equals("true") != getPost)) {
                continue;
            }

            // main part: put the item in
            userPubs.add(ModelHelper.doc2Message(publication, da, userId));
            logger.debug("getPublications (history): added message to return");
            fetchCount++;
            // if any error is thrown during this process, directly throw it to the next level
        }
        return userPubs;
    }

    private final int MOST_LIKES = 1;
    private final int MOST_RECENT = 2;

    private void swap(List<NearMeMessage> heap, int left, int right) {
        NearMeMessage temp = heap.get(left);
        heap.set(left, heap.get(right));
        heap.set(right, temp);
    }

    private boolean minimize(List<NearMeMessage> heap, int filter, int min, int x, List<String> topics) {
        int multiplier = topics.indexOf(heap.get(x).getTopic()) + 2;
        int minMultiplier = topics.indexOf(heap.get(min).getTopic()) + 2;
        switch (filter) {
            case MOST_LIKES:
                return (heap.get(x).getLikes() * multiplier) < (heap.get(min).getLikes() * minMultiplier);
            case MOST_RECENT:
                return (heap.get(x).getTimestamp() * multiplier) < (heap.get(min).getTimestamp() * minMultiplier);
            default:
                return multiplier < minMultiplier;
        }
    }

    private boolean condition(List<NearMeMessage> heap, int filter, int i, int parent, List<String> topics) {
        int multiplier = topics.indexOf(heap.get(i).getTopic()) + 2;
        int parentMultiplier = topics.indexOf(heap.get(parent).getTopic()) + 2;
        switch (filter) {
            case MOST_LIKES:
                return (heap.get(i).getLikes() * multiplier) < (heap.get(parent).getLikes() * parentMultiplier);
            case MOST_RECENT:
                return (heap.get(i).getTimestamp() * multiplier) < (heap.get(parent).getTimestamp() * parentMultiplier);
            default:
                return multiplier < parentMultiplier;
        }
    }

    private void heapify(List<NearMeMessage> heap, int i, int size, int filter, List<String> topics) {
        int left = 2 * i + 1;
        int right = 2 * i + 2;
        int min = i;

        if (left < size && minimize(heap, filter, min, left, topics)) {
            min = left;
        }
        if (right < size && minimize(heap, filter, min, right, topics)) {
            min = right;
        }

        if (min != i) {
            swap(heap, i, min);
            heapify(heap, min, size, filter, topics);
        }
    }

    private int parent(int i) {
        return (i - 1) / 2;
    }

    private List<String> parseTopicFilter(String filter) {
        List<String> temp = new ArrayList<String>(Arrays.asList(filter.split("\n")));
        temp.remove(0);
        return temp;
    }

    private String[] parseFilter(String filter) {
        List<String> temp = new ArrayList<String>(Arrays.asList(filter.split("\n")));
        String tempArray[] = temp.remove(0).split(" ");
        if (tempArray.length < 2) {
            return new String[] {"100", "0"};
        }
        return tempArray;
    }

    @Override
    public NearMeARRes getMessage(NearMeARReq reqParams, String userName) throws Exception {
        logger.info("getMessage (main): received request");

        String userId = da.users.getID(userName);
        if (userId.isEmpty()){
            throw new UserNotFoundException();
        }

        List<String> lastSurroundingMeshs = da.geomesh.getSurroundingGeomesh(reqParams.getLastPos(), SURROUNDING_RADIUS);
        List<String> newMeshs = getDiff(reqParams.getCurrentPos(), lastSurroundingMeshs);
        logger.debug("getMessage (main): geomesh determined");

        List<Document> newPublications = new ArrayList<Document>();
        ArrayList<String> topic_ids = new ArrayList<String>(Arrays.asList(reqParams.getTopics()));

        // get contents from db
        newMeshs.forEach(mesh -> {
            try {
                newPublications.addAll(
                    reqParams.getType() == 1 ?
                    da.messages.getContents(mesh, topic_ids) :
                    da.messages.getComments(mesh, topic_ids)
                );
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });

        logger.debug("getMessage (main): all message retrieved");

        String filterString = reqParams.getFilter();
        String parsedFilter[] = parseFilter(filterString);

        int maxTemp = 100;
        try {
            maxTemp = Integer.parseInt(parsedFilter[0]);
        } catch (NumberFormatException e) {
            // Log? Requester did not send the parameter
        }
        int filter = 0;
        try {
            Integer.parseInt(parsedFilter[1]);
        } catch (NumberFormatException e) {
            // Log? Requester did not send the parameter
        }
        int max = maxTemp;

        List<String> topics = parseTopicFilter(filterString);

        logger.debug("getMessage (main): filters parsed");

        List<NearMeMessage> newContents = new ArrayList<NearMeMessage>();
        newPublications.forEach(doc -> {
            int timestamp = ((Long) doc.get("Timestamp")).intValue();
            int likes = (int) doc.get("Likes");
            int multiplier = topics.indexOf((String) doc.get("Topic")) + 2;
            boolean replace = false;
            if (newContents.size() >= max) {
                int rootMultiplier = topics.indexOf(newContents.get(0).getTopic()) + 2;
                switch (filter) {
                    case MOST_LIKES:
                        // Remove root of min-heap and later add new element, if required
                        replace = (multiplier * likes) > (rootMultiplier * newContents.get(0).getLikes());
                        break;
                    case MOST_RECENT:
                        // Remove root of min-heap and later add new element, if required
                        replace = (multiplier * timestamp) > (rootMultiplier * newContents.get(0).getTimestamp());
                        break;
                    default:
                        replace = multiplier > rootMultiplier;
                        break;
                }
                if (replace) {
                    // Replace min-heap root with copy of last element
                    newContents.set(0, newContents.get(max - 1));
                    // Remove original last element
                    newContents.remove(max - 1);
                    // Heapify the min heap
                    heapify(newContents, 0, max - 1, filter, topics);
                }
            }

            // Add new element to the min-heap
            if (newContents.size() < max || replace) {
                try {
                    newContents.add(ModelHelper.doc2Message(doc, da, userId));
                } catch (UserNotFoundException e) {
                    // TODO Auto-generated catch block
                    e.printStackTrace();
                } catch (Exception e) {
                    throw new RuntimeException(e);
                }

                // Move new element up heap until min-heap again
                int i = newContents.size() - 1;
                while (i != 0 && condition(newContents, filter, i, parent(i), topics)) {
                    swap(newContents, i, parent(i));
                    i = parent(i);
                }
            }
            logger.debug("getMessage (main): one filter iteration completed");
        });
        NearMeARRes answer = new NearMeARRes(newContents.toArray(new NearMeMessage[0]), (int) Instant.now().getEpochSecond(), new String[0], new String[0]);
        logger.debug("getMessage (main): response prepared");
        return answer;
    }

    @Override
    public Boolean addLikes(String userName, String messageId, int count) throws Exception {
        logger.info("addLikes: received request");
        // update likes count for message
        Boolean result = da.messages.updateLikes(messageId, count);

        // update likes count for message owner
        String ownerId = da.messages.read(messageId, "User");
        result &= da.users.incrementTotalLikes(ownerId);

        // add message to user's liked list
        String userId = da.users.getID(userName);
        result &= da.users.addToLikes(userId, messageId, da.messages);

        logger.debug("addLikes: all DB action done");
        return result;
    }

    @Override
    public List<NearMeMessage> getTopicMessages(String topicName, double lat, double lng, String userName) throws Exception {
        logger.info("getTopicMessages: received request");
        // get all messages in this topic and geomesh
        double coord[] = {lat, lng};
        ArrayList<String> surroundingMeshs = da.geomesh.getSurroundingGeomesh(coord, SURROUNDING_RADIUS);

        String topicId = da.topics.getID(topicName);
        List<Document> messageDocs = da.messages.getMessagesInTopic(topicId, surroundingMeshs);

        String userId = da.users.getID(userName);

        logger.debug("getTopicMessages: all DB action done");
        List<NearMeMessage> messages = new ArrayList<NearMeMessage>();
        messageDocs.forEach(doc -> {
            try {
                messages.add(ModelHelper.doc2Message(doc, da, userId));
            } catch (UserNotFoundException e) {
                e.printStackTrace();
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });

        return messages;
    }

    @Override
    public List<NearMeMessage> getNearbyMessages(double lat, double lng, String userName) throws Exception {
        logger.info("getNearbyMessages: received request");
        // get all messages in this topic and geomesh
        double coord[] = {lat, lng};
        ArrayList<String> surroundingMeshs = da.geomesh.getSurroundingGeomesh(coord, SURROUNDING_RADIUS);

        List<Document> messageDocs = da.messages.getMessagesInGeomesh(surroundingMeshs);

        String userId = da.users.getID(userName);

        logger.debug("getNearbyMessages: all DB actions done");
        List<NearMeMessage> messages = new ArrayList<NearMeMessage>();
        messageDocs.forEach(doc -> {
            try {
                messages.add(ModelHelper.doc2Message(doc, da, userId));
            } catch (UserNotFoundException e) {
                e.printStackTrace();
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });

        return messages;
    }

    private List<String> getDiff(double[] currLoc, List<String> prevSurrounding) {
        // get current surrounding geomeshes
        List<String> currSurroundng = da.geomesh.getSurroundingGeomesh(currLoc, SURROUNDING_RADIUS);
        Set<String> set = new HashSet<>();

        // remove the previous surrouning meshes from the result list
        for (String s: prevSurrounding) {
            set.add(s);
        }
        List<String> result = new ArrayList<>();
        for(String s: currSurroundng){
            if (!set.contains(s)) {
                result.add(s);
            }
        }

        // return a list of geomesh ID that's not in the previous surrounding
        return result;
    }

}
