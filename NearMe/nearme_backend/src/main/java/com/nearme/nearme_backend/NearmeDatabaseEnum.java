package com.nearme.nearme_backend;

/**
 * This file is setup mainly to help communicating with database accessor @Aidan
 */

public class NearmeDatabaseEnum {
    public enum Table {
        AR_CONTENT,
        TOPIC,
        USER,
        LOCATION
    }

    public enum Column {
        TOPIC_ID,
        TOPIC_NAME,
        TOPIC_GEOMESH,
        TOPIC_ALL,
        USER_ID,
        USER_NAME,
        USER_SUBLIST,
        USER_PUBLIST,
        USER_LOCLIST,
        USER_ALL,
        ARCONTENT_ID,
        ARCONTENT_GEOMESH,
        ARCONTENT_LOCATION,
        ARCONTENT_TYPE,
        ARCONTENT_AHCHORED,
        ARCONTENT_CONTENT,
        ARCONTENT_TOPIC,
        ARCONTENT_USER,
        ARCONTENT_TIMESTAMP,
        ARCONTENT_DELETED,
        ARCONTENT_LIKES,
        ARCONTENT_ALL,
        LOCATION_ID,
        LOCATION_NAME,
        LOCATION_ADDRESS,
        LOCATION_LON,
        LOCATION_LAT,
        LOCATION_ALL
    }
}
