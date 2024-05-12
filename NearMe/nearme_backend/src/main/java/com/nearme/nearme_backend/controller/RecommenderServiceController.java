package com.nearme.nearme_backend.controller;
import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.nearme.nearme_backend.model.NearMeTopic;
import com.nearme.nearme_backend.service.GeocodingService;
import com.nearme.nearme_backend.service.GeocodingServiceImpl;
import com.nearme.nearme_backend.service.RecommenderService;
import com.nearme.nearme_backend.service.RecommenderServiceImpl;
import com.nearme.nearme_backend.service.UserAuthService;
import com.nearme.nearme_backend.service.UserAuthServiceFirebase;

@RestController
public class RecommenderServiceController {

    private RecommenderService service;
    private GeocodingService geoService;
    private UserAuthService authService;

    public RecommenderServiceController() {
        service = new RecommenderServiceImpl();
        geoService = new GeocodingServiceImpl();
        authService = new UserAuthServiceFirebase();
    }

    @GetMapping(value = "/getRecommendedTopicList")
    public ResponseEntity<List<NearMeTopic>> getRecommendedTopicList(
            @RequestParam("latitude") String latString,
            @RequestParam("longitude") String lngString,
            @RequestHeader("Authorization") String bearerString) {
        try {
            String userName = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));
            String regionName = geoService.getRegion(Float.parseFloat(latString), Float.parseFloat(lngString));
            return new ResponseEntity<List<NearMeTopic>>(
                    service.getRecommendedTopicList(
                            regionName,
                            userName),
                    HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
}
