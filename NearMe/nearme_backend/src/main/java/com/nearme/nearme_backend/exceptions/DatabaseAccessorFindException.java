package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorFindException extends DatabaseAccessorException {
    public DatabaseAccessorFindException (String message, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor  
        super(message, cause);
    }

    public DatabaseAccessorFindException (String key, Object value, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor
        super("Exception trying to find (" + key + ", " + value.toString() + ")", cause);
    }  

    public DatabaseAccessorFindException (String item, String id, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor  
        super("Exception trying to find " + item + " with _id " + id, cause);
    }
    
    public DatabaseAccessorFindException (String item, String id, String action, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor  
        super("Exception trying to find " + item + " with _id " + id + " to " + action, cause);
    }
}
