package com.nearme.nearme_backend.service;

import com.google.firebase.auth.FirebaseAuthException;
import com.google.firebase.auth.UserRecord;
import com.nearme.nearme_backend.model.NearMeUserMeta;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorException;

public interface UserService {
    public UserRecord getFBUser(String userId) throws FirebaseAuthException;
    public NearMeUserMeta getDBUser(String userId) throws DatabaseAccessorException;
    public boolean addUser(String userID) throws DatabaseAccessorException;
    public boolean deleteUser(String userID) throws DatabaseAccessorException;
}
