package com.nearme.nearme_backend.service;

import java.util.ArrayList;
import java.util.List;

import org.bson.Document;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.model.ModelHelper;
import com.nearme.nearme_backend.model.NearMeLocation;

public class BindedLocationServiceImpl implements BindedLocationService {

    DatabaseAccessor da = DatabaseAccessor.getInstance();
    private final double EPSILON = 0.000001;
    Logger logger = LoggerFactory.getLogger(BindedLocationServiceImpl.class);

    @Override
    public void addLocation(double latitude, double longitude, String name, String userId) throws Exception {
        logger.info("addLocation: received request");
        double[] coordinate = {latitude, longitude};
        da.locations.create(name, coordinate);
        da.users.addLocation(da.users.getID(userId), da.locations.getID("address"), da.locations);
    }

    @Override
    public List<NearMeLocation> getLocations(String userId) throws Exception {
        logger.info("getLocations: received request");
        List<String> locations = da.users.getLocations(da.users.getID(userId));
        List<NearMeLocation> userLocations = new ArrayList<NearMeLocation>();
        for (int i = 0; i < locations.size(); i++) {
            String location = locations.get(i);
            Document fullLoc = da.locations.read(location);
            userLocations.add(ModelHelper.doc2Location(fullLoc));
        }
        return userLocations;
    }

    @Override
    public void deleteLocation(String locationId, String userId) throws Exception {
        logger.info("deleteLocation: received request");
        da.users.deleteLocation(da.users.getID(userId), locationId, da.locations);
    }

    @Override
    public boolean isVerified(double latitude, double longitude, String userId) throws Exception {
        logger.info("isVerified: received request");
        // get list of location
        List<String> locs = da.users.getLocations(da.users.getID(userId));
        for (String loc : locs) {
            double[] locCoordinates = da.locations.getCoordinates(loc);
            if (Math.abs(locCoordinates[0] - latitude) < EPSILON && Math.abs(locCoordinates[1] - longitude) < EPSILON) {
                return true;
            }
        }
        return false;
    }

    @Override
    public void updateLocation(String locationId, String newName, String userId) throws Exception {
        logger.info("updateLocation: received request");
        Document original = da.locations.read(locationId);
        original.replace("Name", newName);
        da.locations.update(locationId, original);
    }
}
