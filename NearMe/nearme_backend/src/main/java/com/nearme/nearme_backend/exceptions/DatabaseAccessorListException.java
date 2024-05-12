package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorListException extends DatabaseAccessorException {
    public DatabaseAccessorListException (String message, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor  
        super(message, cause);
    }
}
