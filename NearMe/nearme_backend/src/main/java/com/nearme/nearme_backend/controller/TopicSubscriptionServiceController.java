package com.nearme.nearme_backend.controller;

import java.util.List;
import java.util.Optional;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.nearme.nearme_backend.model.NearMeTopic;
import com.nearme.nearme_backend.service.GeocodingService;
import com.nearme.nearme_backend.service.GeocodingServiceImpl;
import com.nearme.nearme_backend.service.TopicSubscriptionService;
import com.nearme.nearme_backend.service.TopicSubscriptionServiceImpl;
import com.nearme.nearme_backend.service.UserAuthService;
import com.nearme.nearme_backend.service.UserAuthServiceFirebase;

@RestController
public class TopicSubscriptionServiceController {
    private TopicSubscriptionService service;
    private UserAuthService authService;
    private GeocodingService geoService;

    public TopicSubscriptionServiceController() {
        super();
        service = new TopicSubscriptionServiceImpl();
        authService = new UserAuthServiceFirebase();
        geoService = new GeocodingServiceImpl();
    }

    @GetMapping(value = "/addTopic")
    public ResponseEntity<Void> addTopic(
            @RequestParam("name") String name,
            @RequestParam("regionName") String regionName,
            @RequestHeader("Authorization") String bearerString) {
        try {
            String uid = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));
            boolean r = service.addTopic(name, regionName, uid);
            if (r){
                return new ResponseEntity<>(HttpStatus.OK);
            }else{
                // duplicate topic name for this region
                return new ResponseEntity<>(HttpStatus.FORBIDDEN);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }

    }

    @GetMapping(value = "/deleteTopic")
    public ResponseEntity<Void> deleteTopic(
            @RequestParam("topic") String topicID,
            @RequestHeader("Authorization") String bearerString) {
        try {
            String uid = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));
            boolean r = service.deleteTopic(topicID, uid);
            if (r){
                return new ResponseEntity<>(HttpStatus.OK);
            }else{
                return new ResponseEntity<>(HttpStatus.BAD_REQUEST);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }


    @GetMapping(value = "/getTopicList")
    public ResponseEntity<List<NearMeTopic>> getTopicList(
            @RequestParam("latitude") Optional<String> latString,
            @RequestParam("longitude") Optional<String> lngString,
            @RequestParam("uid") Optional<String> uid,
            @RequestHeader("Authorization") String bearerString) {
        try {
            String userId = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));

            if (uid.isPresent()){
                return new ResponseEntity<List<NearMeTopic>>(
                    service.getUserTopicList(uid.get()),
                    HttpStatus.OK);
            }else if (latString.isPresent() && lngString.isPresent()){
                String regionName = geoService.getRegion(Float.parseFloat(latString.get()), Float.parseFloat(lngString.get()));
                return new ResponseEntity<List<NearMeTopic>>(
                    service.getTopicList(regionName,userId),
                    HttpStatus.OK);
            }else{
                return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @PostMapping(value = "/subscription")
    public ResponseEntity<Void> subscribeTopics(@RequestBody List<String> topicList,
            @RequestHeader("Authorization") String bearerString) {
        try {
            String userId = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));
            service.subscribeTopics(topicList, userId);
            return new ResponseEntity<Void>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @DeleteMapping(value = "/subscription")
    public ResponseEntity<Void> unsubscribeTopics(@RequestBody List<String> topicList,
            @RequestHeader("Authorization") String bearerString) {
        try {
            String userId = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));
            service.unsubscribeTopics(topicList, userId);
            return new ResponseEntity<Void>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @GetMapping(value = "/subscription")
    public ResponseEntity<List<NearMeTopic>> getSubscriptions(@RequestHeader("Authorization") String bearerString) {
        try {
            String userId = authService.checkAuth(bearerString.substring(bearerString.indexOf(' ') + 1));
            return new ResponseEntity<List<NearMeTopic>>(service.getSubscriptions(userId), HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

}
