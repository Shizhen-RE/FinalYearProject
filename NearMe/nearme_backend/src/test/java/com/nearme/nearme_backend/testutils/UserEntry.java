package com.nearme.nearme_backend.testutils;

import java.util.Objects;

public class UserEntry {
    private String id;
    private String name;
    private String[] authLocations;
    private String[] subscriptions;
    private String[] publications;

    public UserEntry() {
    }

    public UserEntry(String id, String name, String[] authLocations, String[] subscriptions, String[] publications) {
        this.id = id;
        this.name = name;
        this.authLocations = authLocations;
        this.subscriptions = subscriptions;
        this.publications = publications;
    }

    public String getId() {
        return this.id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getName() {
        return this.name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String[] getAuthLocations() {
        return this.authLocations;
    }

    public void setAuthLocations(String[] authLocations) {
        this.authLocations = authLocations;
    }

    public String[] getSubscriptions() {
        return this.subscriptions;
    }

    public void setSubscriptions(String[] subscriptions) {
        this.subscriptions = subscriptions;
    }

    public String[] getPublications() {
        return this.publications;
    }

    public void setPublications(String[] publications) {
        this.publications = publications;
    }

    public UserEntry id(String id) {
        setId(id);
        return this;
    }

    public UserEntry name(String name) {
        setName(name);
        return this;
    }

    public UserEntry authLocations(String[] authLocations) {
        setAuthLocations(authLocations);
        return this;
    }

    public UserEntry subscriptions(String[] subscriptions) {
        setSubscriptions(subscriptions);
        return this;
    }

    public UserEntry publications(String[] publications) {
        setPublications(publications);
        return this;
    }

    @Override
    public boolean equals(Object o) {
        if (o == this)
            return true;
        if (!(o instanceof UserEntry)) {
            return false;
        }
        UserEntry userEntry = (UserEntry) o;
        return Objects.equals(id, userEntry.id) && Objects.equals(name, userEntry.name) && Objects.equals(authLocations, userEntry.authLocations) && Objects.equals(subscriptions, userEntry.subscriptions) && Objects.equals(publications, userEntry.publications);
    }

    @Override
    public int hashCode() {
        return Objects.hash(id, name, authLocations, subscriptions, publications);
    }

    @Override
    public String toString() {
        return "{" +
            " id='" + getId() + "'" +
            ", name='" + getName() + "'" +
            ", authLocations='" + getAuthLocations() + "'" +
            ", subscriptions='" + getSubscriptions() + "'" +
            ", publications='" + getPublications() + "'" +
            "}";
    }
    
}
