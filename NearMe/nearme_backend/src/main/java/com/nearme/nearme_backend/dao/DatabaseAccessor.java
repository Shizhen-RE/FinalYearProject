package com.nearme.nearme_backend.dao;

import com.google.firestore.v1.DatabaseRootName;
import com.mongodb.*;
import com.mongodb.client.MongoClients;
import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoDatabase;

public class DatabaseAccessor {

    // singleton core
    private static DatabaseAccessor instance = null;
    private static Object mutex = new Object();
    public static DatabaseAccessor getInstance() {
        DatabaseAccessor da = instance;
        if (da == null) {
            synchronized (mutex) {
                da = instance;
                if (da == null) {
                    instance = new DatabaseAccessor();
                    instance.init();
                    da = instance;
                }
            }
        }
        return da;
    }
    // initialization
    private DatabaseAccessor() {
        String databaseString = DatabaseConfig.CONNSTR;
        // String databaseString = "mongodb://localhost:27017"; // local testing

        // Set up MongoDB connection
        ConnectionString connectionString = new ConnectionString(databaseString);

        // Configure and create client
        MongoClientSettings settings = MongoClientSettings.builder()
                .applyConnectionString(connectionString)
                .serverApi(ServerApi.builder()
                    .version(ServerApiVersion.V1)
                    .build())
                .build();
        mongoClient = MongoClients.create(settings);

        // Connect to Main database and get collections
        database = mongoClient.getDatabase(DatabaseConfig.DATABASE);
        messages = new Messages(database);
        locations = new Locations(database);
        regions = new Regions(database);
        topics = new Topics(database);
        users = new Users(database);
        geomesh = new Geomesh(database);
    }

    private void init() {
    }

    // Database client and database
    private MongoClient mongoClient;
    private MongoDatabase database;

    // Database collections
    public Messages messages;
    public Locations locations;
    public Regions regions;
    public Topics topics;
    public Users users;
    public Geomesh geomesh;
}
