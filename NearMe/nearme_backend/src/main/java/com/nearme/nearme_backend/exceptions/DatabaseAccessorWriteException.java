package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorWriteException extends DatabaseAccessorException {
    public DatabaseAccessorWriteException (String message, String action, String id, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor  
        super(message + "trying to " + action + " item with _id " + id, cause);
    }

    public DatabaseAccessorWriteException (String message, String action, Throwable cause) {
        // calling parent DatabaseAccessorException class constructor  
        super(message + "trying to " + action + " item", cause);
    }
}
