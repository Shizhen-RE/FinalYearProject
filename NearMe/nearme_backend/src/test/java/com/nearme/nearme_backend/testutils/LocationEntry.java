package com.nearme.nearme_backend.testutils;

import java.util.Objects;

public class LocationEntry {
    private String id;
    private float[] location;
    private String address;

    public LocationEntry() {
    }

    public LocationEntry(String id, float[] location, String address) {
        this.id = id;
        this.location = location;
        this.address = address;
    }

    public String getId() {
        return this.id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public float[] getLocation() {
        return this.location;
    }

    public void setLocation(float[] location) {
        this.location = location;
    }

    public String getAddress() {
        return this.address;
    }

    public void setAddress(String address) {
        this.address = address;
    }

    public LocationEntry id(String id) {
        setId(id);
        return this;
    }

    public LocationEntry location(float[] location) {
        setLocation(location);
        return this;
    }

    public LocationEntry address(String address) {
        setAddress(address);
        return this;
    }

    @Override
    public boolean equals(Object o) {
        if (o == this)
            return true;
        if (!(o instanceof LocationEntry)) {
            return false;
        }
        LocationEntry locationEntry = (LocationEntry) o;
        return Objects.equals(id, locationEntry.id) && Objects.equals(location, locationEntry.location) && Objects.equals(address, locationEntry.address);
    }

    @Override
    public int hashCode() {
        return Objects.hash(id, location, address);
    }

    @Override
    public String toString() {
        return "{" +
            " id='" + getId() + "'" +
            ", location='" + getLocation() + "'" +
            ", address='" + getAddress() + "'" +
            "}";
    }

}
