package com.nearme.nearme_backend.service;

import static org.junit.Assert.*;

import org.junit.Test;

public class GeocodingServiceTest {
    
    double latitude = 43.472317f;
    double longitude = -80.544847f;

    GeocodingServiceImpl service = new GeocodingServiceImpl();

    @Test
    public void testGetRegionName(){
        try {
            String regionName = service.getRegion(latitude, longitude);
            assertEquals("Waterloo,ON,CA", regionName);
            
        } catch (Exception e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
    }

    @Test
    public void testGetStreetAddress(){
        try {
            String address = service.getStreetAddress(latitude, longitude);
            assertEquals("200 University Ave W, Waterloo, ON N2L 3G1, Canada", address);
            
        } catch (Exception e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
    }
}
