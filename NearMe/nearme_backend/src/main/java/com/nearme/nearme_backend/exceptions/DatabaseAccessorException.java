package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorException extends Exception {
    public DatabaseAccessorException (String message, Throwable cause) {
        // calling parent Exception class constructor  
        super(message, cause);
    }
}
