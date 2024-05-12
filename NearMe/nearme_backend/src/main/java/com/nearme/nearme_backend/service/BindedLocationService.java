package com.nearme.nearme_backend.service;

import java.util.List;

import com.nearme.nearme_backend.model.NearMeLocation;

public interface BindedLocationService {
    public abstract void addLocation(double latitude, double longitude, String name, String userId) throws Exception;
    public abstract void updateLocation(String locationId, String newName, String userId) throws Exception;
    public abstract List<NearMeLocation> getLocations(String userId) throws Exception;
    public abstract void deleteLocation(String locationId, String userId) throws Exception;
    public abstract boolean isVerified(double latitude, double longitude, String userId) throws Exception;
}
