package com.nearme.nearme_backend.service;

import org.bson.Document;

import java.util.ArrayList;

import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseAuthException;
import com.google.firebase.auth.UserRecord;
import com.nearme.nearme_backend.dao.DatabaseAccessor;
import com.nearme.nearme_backend.exceptions.DatabaseAccessorException;
import com.nearme.nearme_backend.model.NearMeUserMeta;

public class UserServiceImpl implements UserService{
    DatabaseAccessor da = DatabaseAccessor.getInstance();

    @Override
    public boolean addUser(String userID) throws DatabaseAccessorException {
        // return true if user added to db
        // return false if user already in db
        return !da.users.create(userID).isEmpty();
    }

    @Override
    public boolean deleteUser(String userID) throws DatabaseAccessorException {
        // return true if user deleted from db
        // return false if user does not exist in db
        return (da.users.delete(userID) == null);
    }

    @Override
    public UserRecord getFBUser(String firebaseUID) throws FirebaseAuthException {
        return FirebaseAuth.getInstance().getUser(firebaseUID);
    }

    @Override
    public NearMeUserMeta getDBUser(String firebaseUID) throws DatabaseAccessorException {
        Document userdoc = da.users.read(da.users.getID("Name", firebaseUID));
        return new NearMeUserMeta(
            userdoc.getObjectId("_id").toHexString(),
            userdoc.getString("Name"), 
            userdoc.getInteger("TotalLikes", 0), 
            userdoc.getList("Publications", String.class).size(), 
            userdoc.getList("Subscriptions", String.class).size(), 
            userdoc.getList("Topics", String.class).size()
        );
    }
}
