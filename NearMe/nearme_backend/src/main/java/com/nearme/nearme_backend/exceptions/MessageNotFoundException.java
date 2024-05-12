package com.nearme.nearme_backend.exceptions;

public class MessageNotFoundException extends Exception{
    public static String message = "Message does not exist in the database.";

    public MessageNotFoundException () {
        // calling parent Exception class constructor  
        super(message);    
    }    
}
