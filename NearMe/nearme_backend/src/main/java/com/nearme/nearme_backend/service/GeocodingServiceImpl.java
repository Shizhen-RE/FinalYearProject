package com.nearme.nearme_backend.service;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import org.json.JSONArray;
import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class GeocodingServiceImpl implements GeocodingService {

    String API_KEY = "AIzaSyC2Ia4Wq2Zm2K6aAKQ-4fZDm1ogROWDCWQ"; // required
    String GEOCODING_API = "https://maps.googleapis.com/maps/api/geocode/json"; // base URL
    Logger logger = LoggerFactory.getLogger(GeocodingServiceImpl.class);

    @Override
    public String getRegion(double latitude, double longitude) throws Exception {
        logger.debug("getRegion: entrance");

        if (latitude == 0 && longitude == 0){
            // testing
            return "testregion";
        }
        
        // call Google Geocoding API
        JSONObject resJson = reverseGeocoding(latitude, longitude);

        // parse the first result, should be the best match
        JSONObject result = (JSONObject)resJson.getJSONArray("results").get(0);
        JSONArray componentsList = (JSONArray)result.getJSONArray("address_components");
        String city = "";
        String province = ""; 
        String country = "";
        for (int i=0; i<componentsList.length(); i++){ 
            JSONObject component = (JSONObject)componentsList.get(i);
            JSONArray types = (JSONArray)component.getJSONArray("types");
            for (int k=0; k<types.length(); k++){ 
                String type = (String)types.get(k);
                if (type.equals("locality")){
                    city = (String)component.get("long_name");
                }else if(type.equals("administrative_area_level_1")){
                    province = (String)component.get("short_name");
                }else if(type.equals("country")){
                    country = (String)component.get("short_name");
                }
            }
        }

        if (city == "" && province == "" && country == ""){
            String msg = "Cannot get region name for geo coordinates (" + Double.toString(latitude) + "," + Double.toString(longitude) + ")";
            throw new Exception(msg);
        }else{
            String region = Stream.of(city, province, country)
                            .filter(s -> s != null && !s.isEmpty())
                            .collect(Collectors.joining(","));
            logger.debug("getRegion: done");
            return region;
        }
    }

    @Override
    public String getStreetAddress(double latitude, double longitude) throws Exception {
        // call Google Geocoding API
        JSONObject resJson = reverseGeocoding(latitude, longitude);

        // parse the first result
        JSONObject result = (JSONObject) resJson.getJSONArray("results").get(0);
        return (String)result.get("formatted_address");
    }

    private JSONObject reverseGeocoding(double latitude, double longitude){
        logger.trace("Geocoding service: reverse geocoding");
        // build the URL
        String URL = GEOCODING_API 
                    + "?latlng=" + Double.toString(latitude) + "," + Double.toString(longitude)
                    + "&key=" + API_KEY;
        
        // make the request
        try {
            // build request
            HttpClient client = HttpClient.newHttpClient();
            HttpRequest request = HttpRequest.newBuilder().uri(URI.create(URL)).build();

            // send http request
            HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());

            // check response code
            if (response.statusCode() != 200){
                System.err.println("Reverse geocoding request failed with response code " + response.statusCode());
            }else{
                logger.trace("Geocoding service: request to google successful");
                // ok, return response json object
                return new JSONObject(response.body());
            }

        } catch (Exception e) {
            e.printStackTrace();
        }

        return null;
    }

}
