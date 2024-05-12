package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorWriteConcernException extends DatabaseAccessorWriteException {
    public DatabaseAccessorWriteConcernException (String action, String id, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Unable to fulfil write concern ", action, id, cause);
    }

    public DatabaseAccessorWriteConcernException (String action, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Unable to fulfil write concern ", action, cause);
    }
}
