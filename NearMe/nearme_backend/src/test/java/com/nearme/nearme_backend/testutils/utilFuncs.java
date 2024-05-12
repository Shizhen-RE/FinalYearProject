package com.nearme.nearme_backend.testutils;

import java.util.ArrayList;
import java.util.List;

public class utilFuncs {
    public static List<UserEntry> initUsers() {
        String[] user1Loc = {};
        String[] user1Sub = {"topic1", "topic2"};
        String[] user1Pub = {"message1", "message2", "message3", "message4"};
        UserEntry user1 = new UserEntry("user1", "Sean Li", user1Loc, user1Sub, user1Pub);
        List<UserEntry> testUsers = new ArrayList<UserEntry>();
        testUsers.add(user1);
        return testUsers;
    }
}
