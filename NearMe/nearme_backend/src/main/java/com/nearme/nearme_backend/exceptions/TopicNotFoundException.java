package com.nearme.nearme_backend.exceptions;

public class TopicNotFoundException  extends Exception{
    public static String message = "Topic does not exist in the database.";

    public TopicNotFoundException () {
        // calling parent Exception class constructor  
        super(message);    
    }    
}
