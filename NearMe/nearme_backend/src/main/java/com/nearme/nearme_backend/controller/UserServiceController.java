package com.nearme.nearme_backend.controller;


import java.util.Optional;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.nearme.nearme_backend.service.UserServiceImpl;
import com.google.firebase.auth.UserRecord;
import com.nearme.nearme_backend.model.NearMeUserMeta;
import com.nearme.nearme_backend.service.UserAuthService;
import com.nearme.nearme_backend.service.UserAuthServiceFirebase;

@RestController
public class UserServiceController {

    private UserAuthService authService;
    private UserServiceImpl service;

    public UserServiceController() {
        super();
        service = new UserServiceImpl();
        authService = new UserAuthServiceFirebase();
    }

    @GetMapping(value = "/getFBUser")
    public ResponseEntity<UserRecord> getFBUser(
        @RequestHeader("Authorization") String bearerToken,
        @RequestParam("uid") String firebaseUID){
        try {
            String requestor = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            if (requestor != null){
                return new ResponseEntity<UserRecord>(service.getFBUser(firebaseUID), HttpStatus.OK);
            }else{
                return new ResponseEntity<>(HttpStatus.BAD_REQUEST);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @GetMapping(value = "/user")
    public ResponseEntity<NearMeUserMeta> getDBUser(
        @RequestHeader("Authorization") String bearerToken,
        @RequestParam("uid") Optional<String> firebaseUID){
        try {
            String requestor = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            if (requestor != null){
                if (firebaseUID.isPresent()){
                    return new ResponseEntity<NearMeUserMeta>(service.getDBUser(firebaseUID.get()), HttpStatus.OK);
                }else{
                    return new ResponseEntity<NearMeUserMeta>(service.getDBUser(requestor), HttpStatus.OK);
                }
                
            }else{
                return new ResponseEntity<>(HttpStatus.BAD_REQUEST);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    @PostMapping(value = "/user")
    public ResponseEntity<Void> addUser(@RequestHeader("Authorization") String bearerToken){
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            if (service.addUser(userId)){
                return new ResponseEntity<>(HttpStatus.OK);
            }else{
                return new ResponseEntity<>(HttpStatus.BAD_REQUEST);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }

    // Assume only user can delete themselves
    @DeleteMapping(value = "/user")
    public ResponseEntity<Void> deleteUser(@RequestHeader("Authorization") String bearerToken){
        try {
            String userId = authService.checkAuth(bearerToken.substring(bearerToken.indexOf(' ') + 1));
            if (service.deleteUser(userId)) {
                return new ResponseEntity<>(HttpStatus.OK);
            }else{
                return new ResponseEntity<>(HttpStatus.NOT_FOUND);
            }
        } catch (Exception e) {
            e.printStackTrace();
            return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
        }
    }
}
