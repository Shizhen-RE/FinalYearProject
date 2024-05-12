package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorWriteSpecificException extends DatabaseAccessorWriteException {
    public DatabaseAccessorWriteSpecificException (String action, String id, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Specific write exception ", action, id, cause);
    }

    public DatabaseAccessorWriteSpecificException (String action, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Specific write exception ", action, cause);
    }
}
