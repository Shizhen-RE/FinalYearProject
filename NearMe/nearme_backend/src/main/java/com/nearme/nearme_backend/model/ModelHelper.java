package com.nearme.nearme_backend.model;

import java.util.List;

import org.bson.Document;

import com.nearme.nearme_backend.dao.DatabaseAccessor;

public class ModelHelper {
    private static double[] dList2dArr(List<Double> dList){
        double[] ret = new double[dList.size()];
        for (int i = 0; i < dList.size(); i++) {
            ret[i] = dList.get(i);
        }
        return ret;
    }

    private static float[] fList2fArr(List<Float> fList) {
        float[] ret = new float[fList.size()];
        for (int i = 0; i < fList.size(); i++) {
            ret[i] = fList.get(i);
        }
        return ret;
    }

    private static float[] dList2fArr(List<Double> dList){
        float[] ret = new float[dList.size()];
        for (int i = 0; i < dList.size(); i++) {
            ret[i] = dList.get(i).floatValue();
        }
        return ret;
    }

    public static NearMeLocation doc2Location(Document doc) throws Exception {
        try {
            return new NearMeLocation(
                doc.getObjectId("_id").toHexString(),
                doc.getString("Name"),
                doc.getString("Address"),
                dList2dArr(doc.getList("Coordinates", Double.class))
            );
        } catch (Exception e) {
            throw new Exception("Error filling fields. Type mismatch?");
        }
    }

    public static NearMeTopic doc2Topic(Document doc) throws Exception {
        try {
            return new NearMeTopic(
                doc.getObjectId("_id").toHexString(),
                doc.getString("Name"),
                doc.getString("RegionId"),
                doc.getLong("Timestamp"),
                doc.getString("CreatedBy"),
                doc.getInteger("MessageCount", 0),
                doc.getInteger("SubscriptionCount", 0)
            );
        } catch (Exception e) {
            throw new Exception("Error filling fields. Type mismatch?");
        }

    }

    public static NearMeMessage doc2Message(Document doc, DatabaseAccessor da, String myId) throws Exception {
        try {
            return new NearMeMessage(
                doc.getObjectId("_id").toHexString(),
                da.users.getName(doc.getString("User")),    // return user name
                da.topics.getName(doc.getString("Topic")),  // return topic name
                dList2dArr(doc.getList("Location", Double.class)),
                doc.getLong("Timestamp"),
                doc.getInteger("Likes", 0),
                da.users.likedMessage(myId, doc.getObjectId("_id").toHexString()),
                doc.getString("Content"),
                doc.getString("Anchored").equals("true"),
                dList2fArr(doc.getList("Size", Double.class)),
                doc.getString("ImageFormat"),
                doc.getString("Preview"),
                doc.getInteger("Color"),
                doc.getString("Style"),
                doc.getDouble("Scale").floatValue(),
                dList2fArr(doc.getList("Rotation", Double.class))
            );
        } catch (Exception e) {
            e.printStackTrace();
            throw new Exception("Error filling fields. Type mismatch?");
        }

    }
}
