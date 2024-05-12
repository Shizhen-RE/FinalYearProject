package com.nearme.nearme_backend.exceptions;

public class DatabaseAccessorWriteCommandException extends DatabaseAccessorWriteException {
    public DatabaseAccessorWriteCommandException (String action, String id, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Unable to fulfil write concern ", action, id, cause);
    }

    public DatabaseAccessorWriteCommandException (String action, Throwable cause) {
        // calling parent DatabaseAccessorWriteException class constructor  
        super("Unable to fulfil write concern ", action, cause);
    }
}
