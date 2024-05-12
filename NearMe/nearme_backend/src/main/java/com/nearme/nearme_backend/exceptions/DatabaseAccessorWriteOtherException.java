package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorWriteOtherException extends DatabaseAccessorWriteException {
    public DatabaseAccessorWriteOtherException (String action, String id, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Other exception ", action, id, cause);
    }

    public DatabaseAccessorWriteOtherException (String action, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Other exception ", action, cause);
    }
}
