package com.nearme.nearme_backend;

import java.io.FileInputStream;
import java.io.IOException;
import java.util.List;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

import com.google.auth.oauth2.GoogleCredentials;
import com.google.firebase.FirebaseApp;
import com.google.firebase.FirebaseOptions;

@SpringBootApplication
@EnableScheduling
public class NearmeBackendApplication {

    public static void main(String[] args) {
        // firebase initialization, based on doc this should be called only once before anything happen
        try {
            FirebaseOptions options = FirebaseOptions.builder()
                .setCredentials(GoogleCredentials.getApplicationDefault())
                .setDatabaseUrl("https://nearmefirebase-default-rtdb.firebaseio.com/")
                .build();

			boolean hasBeenInitialized=false;
			List<FirebaseApp> firebaseApps = FirebaseApp.getApps();
			for(FirebaseApp app : firebaseApps){
				if(app.getName().equals(FirebaseApp.DEFAULT_APP_NAME)){
					hasBeenInitialized=true;
				}
			}

			if(!hasBeenInitialized) {
				FirebaseApp.initializeApp(options);
			}
        } catch (IOException e) {
            e.printStackTrace();
            System.out.println("Remember to set the GOOGLE_APPLICATION_CREDENTIALS environment variable");
            // don't exit in case auth is not needed
        } catch (IllegalStateException e) {
            // already initialized
        }

        SpringApplication.run(NearmeBackendApplication.class, args);
    }

}
