package com.nearme.nearme_backend.controller;

import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

import com.nearme.nearme_backend.model.IsVerifiedRes;
import com.nearme.nearme_backend.model.NearMeLocation;
import com.nearme.nearme_backend.service.BindedLocationService;
import com.nearme.nearme_backend.service.BindedLocationServiceImpl;
import com.nearme.nearme_backend.service.UserAuthService;
import com.nearme.nearme_backend.service.UserAuthServiceFirebase;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;

@RestController
public class BindedLocationServiceController {
    // Controller level workflow:
    // authService -> solve for user
    // geoService -> solve for region
    // geomeshService -> solve for geomesh  ???
    // pass to lower service block

    private BindedLocationService service;
    private UserAuthService authService;

    public BindedLocationServiceController() {
        service = new BindedLocationServiceImpl();
        authService = new UserAuthServiceFirebase();
    }

    @GetMapping(value="/isVerified")
    public ResponseEntity<IsVerifiedRes> getMethodName(
        @RequestParam("latitude") String lat,
        @RequestParam("longitude") String lng,
        @RequestParam("altitude") String alt,
        @RequestHeader("Authorization") String bearerToken
    ) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            IsVerifiedRes result = new IsVerifiedRes(service.isVerified(Double.parseDouble(lat), Double.parseDouble(lng), userId));
            return new ResponseEntity<IsVerifiedRes>(result, HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
    
    @GetMapping(value="/location")
    public ResponseEntity<List<NearMeLocation>> getMethodName(@RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            return new ResponseEntity<List<NearMeLocation>>(service.getLocations(userId), HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
    
    @DeleteMapping(value="/location")
    public ResponseEntity<Void> deleteLocation(@RequestParam("location_id") String locId, @RequestHeader("Authorization") String bearerToken) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            service.deleteLocation(locId, userId);
            return new ResponseEntity<Void>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @PutMapping(value="/location")
    public ResponseEntity<Void> updateLocation(
        @RequestParam("location_id") String locId,
        @RequestParam("new_name") String newName,
        @RequestHeader("Authorization") String bearerToken
    ) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            service.updateLocation(locId, newName, userId);
            return new ResponseEntity<Void>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
    
    @PostMapping(value="/location", consumes=MediaType.MULTIPART_FORM_DATA_VALUE)
    public ResponseEntity<Void> addLocation(
        @RequestParam("SupportingDoc") MultipartFile doc,
        @RequestParam("latitude") String lat,
        @RequestParam("longitude") String lng,
        @RequestParam("altitude") String alt,
        @RequestParam("name") String addrName,
        @RequestHeader("Authorization") String bearerToken
    ) {
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            service.addLocation(Double.parseDouble(lat), Double.parseDouble(lng), addrName, userId);
            // TODO add location on every step is not appropriate right now
            return new ResponseEntity<Void>(HttpStatus.OK);
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
}
