package com.nearme.nearme_backend.service;

import java.util.ArrayList;
import java.util.List;
import java.util.Stack;
import java.util.concurrent.TimeUnit;

import org.bson.Document;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import com.nearme.nearme_backend.dao.DatabaseAccessor;

@Component
public class GeomeshServiceImpl implements GeomeshService {

    DatabaseAccessor da = DatabaseAccessor.getInstance();
    
    final static long UPDATE_RATE = TimeUnit.HOURS.toMillis(4); // update every 4 hours
    final static long MAX_MSG_PER_GRID = 100;
    final static double MIN_GRID_SIZE = (double) 10e-6; // about 111m length
    Logger logger = LoggerFactory.getLogger(GeomeshServiceImpl.class);

    // if the number of messages in one geomesh reaches a threshold, split geomesh into 4
    @Override
    @Scheduled(fixedRate = 14400000)  // update geomesh per 4 hour
    public void updateGeomesh() throws Exception{
        logger.warn("=============== Update geomesh is running");

        // count messages added per geomesh in the last 4 hours
        ArrayList<Document> increasedGeomeshCount = da.messages.getIncreasedCountPerGeomesh((System.currentTimeMillis()/1000) - UPDATE_RATE);

        // create a stack of geomesh to check for split condition
        Stack<String> geomeshStack = new Stack<String>();
        for (int i = 0; i < increasedGeomeshCount.size(); i ++) {
            geomeshStack.push(increasedGeomeshCount.get(i).getString("_id"));
        }
        logger.info("Geomesh stack size: " + geomeshStack.size());
        
        // for each geomesh in the stack, check if can split it
        while (!geomeshStack.empty()){
            String id = geomeshStack.pop();
            long messageCount = da.messages.countMessageInGeomesh(id);
            double meshLength = da.geomesh.getLength(id);
            logger.debug("Attempting to split geomesh " + id);
    
            if (messageCount > MAX_MSG_PER_GRID && meshLength > MIN_GRID_SIZE){
                logger.trace("Spliting geomesh " + id);

                // get all messages in this mesh before spliting
                ArrayList<Document> messages = da.messages.getMessagesInGeomesh(id);

                // split current geomesh into 4
                String[] splitGeomesh = da.geomesh.splitGeomesh(id);

                // update messages assigned to the old geomesh id
                for (int i = 0; i < 4; i ++) {
                    Document geomeshDoc = da.geomesh.getGeomesh(splitGeomesh[i]);
                    double latitude = geomeshDoc.getDouble("Latitude");
                    double longitude = geomeshDoc.getDouble("Longitude");
                    double length = geomeshDoc.getDouble("Length");
                    int updatedCount = 0;
                    // get location of each message to reassign geomesh
                    // TODO: optimize this
                    for (int j = 0; j < messages.size(); j ++) {
                        // List [latitude, longitude, altitude], only use lat and lon
                        List<Double> messageCoord = messages.get(j).getList("Location", Double.class);
                        if (messageCoord.get(0) <= latitude && messageCoord.get(0) > (latitude - length)
                          && messageCoord.get(1) >= longitude && messageCoord.get(1) < (longitude + length)) {
                            
                            boolean updated = da.messages.update(messages.get(j).get("_id").toString(), "Geomesh", geomeshDoc.get("_id").toString());
            
                            if (!updated){
                                logger.error("Failed to assign message " + messages.get(j).get("_id").toString() 
                                                    + " to new geomesh " + geomeshDoc.get("_id").toString());
                            }else{
                                updatedCount += 1;
                            }
                        }
                    }
                    logger.trace("Successfully assigned " + updatedCount + " to new geomesh " + geomeshDoc.get("_id").toString());

                    // push the new meshes onto stack for further check
                    geomeshStack.push(splitGeomesh[i]);
                }

            }
            logger.debug("geomesh splitted / no need");
        }
        logger.info("new geomesh stack size: " + geomeshStack.size());
        
        logger.warn("=============== Update geomesh is finished");
    }
    
}
