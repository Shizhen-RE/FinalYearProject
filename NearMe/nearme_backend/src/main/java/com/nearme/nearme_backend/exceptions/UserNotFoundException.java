package com.nearme.nearme_backend.exceptions;

public class UserNotFoundException extends Exception{
    public static String message = "User does not exist in the database.";

    public UserNotFoundException () {
        // calling parent Exception class constructor  
        super(message);    
    }    
}
