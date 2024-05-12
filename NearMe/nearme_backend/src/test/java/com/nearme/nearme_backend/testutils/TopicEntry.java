package com.nearme.nearme_backend.testutils;

import java.util.Objects;

public class TopicEntry {
    private String id;
    private String name;

    public TopicEntry() {
    }

    public TopicEntry(String id, String name) {
        this.id = id;
        this.name = name;
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

    public TopicEntry id(String id) {
        setId(id);
        return this;
    }

    public TopicEntry name(String name) {
        setName(name);
        return this;
    }

    @Override
    public boolean equals(Object o) {
        if (o == this)
            return true;
        if (!(o instanceof TopicEntry)) {
            return false;
        }
        TopicEntry topicEntry = (TopicEntry) o;
        return Objects.equals(id, topicEntry.id) && Objects.equals(name, topicEntry.name);
    }

    @Override
    public int hashCode() {
        return Objects.hash(id, name);
    }

    @Override
    public String toString() {
        return "{" +
            " id='" + getId() + "'" +
            ", name='" + getName() + "'" +
            "}";
    }

}
