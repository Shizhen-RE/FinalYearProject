package com.nearme.nearme_backend.controller;

import org.springframework.web.bind.annotation.RestController;

import com.nearme.nearme_backend.dao.Geomesh;
import com.nearme.nearme_backend.model.NearMeARRes;
import com.nearme.nearme_backend.model.NearMeARReq;
import com.nearme.nearme_backend.model.NearMeMessage;
import com.nearme.nearme_backend.service.GeocodingService;
import com.nearme.nearme_backend.service.GeocodingServiceImpl;
import com.nearme.nearme_backend.service.GeomeshService;
import com.nearme.nearme_backend.service.GeomeshServiceImpl;
import com.nearme.nearme_backend.service.NearMeContentService;
import com.nearme.nearme_backend.service.NearMeContentServiceImpl;
import com.nearme.nearme_backend.service.UserAuthService;
import com.nearme.nearme_backend.service.UserAuthServiceFirebase;

import java.util.List;
import java.util.Optional;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;

@RestController
public class NearMeContentServiceController {
    private NearMeContentService service;
    private UserAuthService authService;
    private GeocodingService geoService;
    private GeomeshService geomeshService;

    public NearMeContentServiceController() {
        service = new NearMeContentServiceImpl();
        authService = new UserAuthServiceFirebase();
        geoService = new GeocodingServiceImpl();
        geomeshService = new GeomeshServiceImpl();
    }

    @GetMapping(value="/healthCheck")
    public ResponseEntity<String> healthCheck(){
        return new ResponseEntity<String>("ok", HttpStatus.OK);
    }

    @PostMapping(value="/nearme")
    public ResponseEntity<NearMeARRes> getMessage(@RequestBody NearMeARReq reqParams, @RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            return new ResponseEntity<NearMeARRes>(service.getMessage(reqParams, userId), HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @PostMapping(value="/message")
    public ResponseEntity<Void> addMessage(@RequestBody NearMeMessage message, @RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            //String userId = "udSGwJxV7JbyMajxeZjvTVqSWW93"; // user@example.com
            String regionName = geoService.getRegion(message.getCoordinates()[0], message.getCoordinates()[1]);
            service.addMessage(message, userId, regionName);
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @PutMapping(value="/message")
    public ResponseEntity<Void> editMessage(@RequestBody NearMeMessage message, @RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            service.editMessage(message, userId);
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @DeleteMapping(value="/message")
    public ResponseEntity<Void> deleteMessage(@RequestParam("message_id") String messageId, @RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            service.deleteMessage(messageId, userId);
            return new ResponseEntity<>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @GetMapping(value="/updateLikes")
    public ResponseEntity<Void> likeMessage( @RequestHeader("Authorization") String bearerToken,
            @RequestParam("message_id") String messageId, 
            @RequestParam("add") int count) {
        try {
            String userName = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
    
            if (userName != null){
                service.addLikes(userName, messageId, count);
                return new ResponseEntity<>(HttpStatus.OK);
            
            }else{
                // cannot authenticate requestor, return http 400
                return new ResponseEntity<>(HttpStatus.BAD_REQUEST);
            }
        
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
    
    @GetMapping(value="/message")
    public ResponseEntity<List<NearMeMessage>> getMessage(
        @RequestParam("start-time") String startTime,
        @RequestParam("count") String countStr,
        @RequestParam("type") String typeStr,
        @RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            return new ResponseEntity<List<NearMeMessage>>(
                service.getPublications(
                    Integer.parseInt(startTime),
                    Integer.parseInt(countStr),
                    userId,
                    typeStr.equals("1")
                ),
                HttpStatus.OK
            );
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @GetMapping(value = "/nearme")
    public ResponseEntity<List<NearMeMessage>> getTopicMessages(
        @RequestHeader("Authorization") String bearerToken,
        @RequestParam("topic") Optional<String> topicName,
        @RequestParam("lat") double latitude,
        @RequestParam("lon") double longitude)
    {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            return new ResponseEntity<List<NearMeMessage>>(
                    topicName.isPresent() ? 
                    service.getTopicMessages(topicName.get(), latitude, longitude, userId) :
                    service.getNearbyMessages(latitude, longitude, userId),
                    HttpStatus.OK
                );
        
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
}
