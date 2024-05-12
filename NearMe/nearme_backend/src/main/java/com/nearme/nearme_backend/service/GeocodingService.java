package com.nearme.nearme_backend.service;

public interface GeocodingService {
    public abstract String getRegion(double latitude, double longitude) throws Exception;
    public abstract String getStreetAddress(double latitude, double longitude) throws Exception;
}
