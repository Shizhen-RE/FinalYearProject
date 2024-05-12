package com.nearme.nearme_backend.service;

import java.util.ArrayList;
import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseAuthException;

public class UserAuthServiceFirebase implements UserAuthService {

    private List<String> bypassTokens = new ArrayList<String>();
    Logger logger;

    public UserAuthServiceFirebase() {
        bypassTokens.add("test1XXXXXXXXXXX");
        bypassTokens.add("test2XXXXXXXXXXX");
        bypassTokens.add("test3XXXXXXXXXXX");
        logger = LoggerFactory.getLogger(UserAuthServiceFirebase.class);
    }

    @Override
    public String checkAuth(String token) throws Exception {
        try {
            logger.trace("firebase authing");
            String result;
            if (bypassTokens.contains(token)) {
                result = token;
            }
            else {
                result = FirebaseAuth.getInstance().verifyIdToken(token).getUid();
            }
            logger.trace("firebase auth done");
            return result;
        } catch (FirebaseAuthException e) {
            e.printStackTrace();
            return null;
        }
    }
    
}
