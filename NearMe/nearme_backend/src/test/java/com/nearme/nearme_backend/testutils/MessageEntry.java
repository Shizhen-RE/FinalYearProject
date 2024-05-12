package com.nearme.nearme_backend.testutils;

import java.util.Objects;

public class MessageEntry {
    private String id;
    private String geomesh;
    private float[] location;
    private String type;
    private String anchored;
    private float[] size;
    private String content;
    private String preview;
    private String topic;
    private String user;
    private int timeStamp;
    private String deleted;
    private int likes;

    public MessageEntry() {
    }

    public MessageEntry(String id, String geomesh, float[] location, String type, String anchored, float[] size, String content, String preview, String topic, String user, int timeStamp, String deleted, int likes) {
        this.id = id;
        this.geomesh = geomesh;
        this.location = location;
        this.type = type;
        this.anchored = anchored;
        this.size = size;
        this.content = content;
        this.preview = preview;
        this.topic = topic;
        this.user = user;
        this.timeStamp = timeStamp;
        this.deleted = deleted;
        this.likes = likes;
    }

    public String getId() {
        return this.id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getGeomesh() {
        return this.geomesh;
    }

    public void setGeomesh(String geomesh) {
        this.geomesh = geomesh;
    }

    public float[] getLocation() {
        return this.location;
    }

    public void setLocation(float[] location) {
        this.location = location;
    }

    public String getType() {
        return this.type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public String getAnchored() {
        return this.anchored;
    }

    public void setAnchored(String anchored) {
        this.anchored = anchored;
    }

    public float[] getSize() {
        return this.size;
    }

    public void setSize(float[] size) {
        this.size = size;
    }

    public String getContent() {
        return this.content;
    }

    public void setContent(String content) {
        this.content = content;
    }

    public String getPreview() {
        return this.preview;
    }

    public void setPreview(String preview) {
        this.preview = preview;
    }

    public String getTopic() {
        return this.topic;
    }

    public void setTopic(String topic) {
        this.topic = topic;
    }

    public String getUser() {
        return this.user;
    }

    public void setUser(String user) {
        this.user = user;
    }

    public int getTimeStamp() {
        return this.timeStamp;
    }

    public void setTimeStamp(int timeStamp) {
        this.timeStamp = timeStamp;
    }

    public String getDeleted() {
        return this.deleted;
    }

    public void setDeleted(String deleted) {
        this.deleted = deleted;
    }

    public int getLikes() {
        return this.likes;
    }

    public void setLikes(int likes) {
        this.likes = likes;
    }

    public MessageEntry id(String id) {
        setId(id);
        return this;
    }

    public MessageEntry geomesh(String geomesh) {
        setGeomesh(geomesh);
        return this;
    }

    public MessageEntry location(float[] location) {
        setLocation(location);
        return this;
    }

    public MessageEntry type(String type) {
        setType(type);
        return this;
    }

    public MessageEntry anchored(String anchored) {
        setAnchored(anchored);
        return this;
    }

    public MessageEntry size(float[] size) {
        setSize(size);
        return this;
    }

    public MessageEntry content(String content) {
        setContent(content);
        return this;
    }

    public MessageEntry preview(String preview) {
        setPreview(preview);
        return this;
    }

    public MessageEntry topic(String topic) {
        setTopic(topic);
        return this;
    }

    public MessageEntry user(String user) {
        setUser(user);
        return this;
    }

    public MessageEntry timeStamp(int timeStamp) {
        setTimeStamp(timeStamp);
        return this;
    }

    public MessageEntry deleted(String deleted) {
        setDeleted(deleted);
        return this;
    }

    public MessageEntry likes(int likes) {
        setLikes(likes);
        return this;
    }

    @Override
    public boolean equals(Object o) {
        if (o == this)
            return true;
        if (!(o instanceof MessageEntry)) {
            return false;
        }
        MessageEntry messageEntry = (MessageEntry) o;
        return Objects.equals(id, messageEntry.id) && geomesh == messageEntry.geomesh && Objects.equals(location, messageEntry.location) && Objects.equals(type, messageEntry.type) && Objects.equals(anchored, messageEntry.anchored) && Objects.equals(size, messageEntry.size) && Objects.equals(content, messageEntry.content) && Objects.equals(preview, messageEntry.preview) && Objects.equals(topic, messageEntry.topic) && Objects.equals(user, messageEntry.user) && timeStamp == messageEntry.timeStamp && Objects.equals(deleted, messageEntry.deleted) && likes == messageEntry.likes;
    }

    @Override
    public int hashCode() {
        return Objects.hash(id, geomesh, location, type, anchored, size, content, preview, topic, user, timeStamp, deleted, likes);
    }

    @Override
    public String toString() {
        return "{" +
            " id='" + getId() + "'" +
            ", geomesh='" + getGeomesh() + "'" +
            ", location='" + getLocation() + "'" +
            ", type='" + getType() + "'" +
            ", anchored='" + getAnchored() + "'" +
            ", size='" + getSize() + "'" +
            ", content='" + getContent() + "'" +
            ", preview='" + getPreview() + "'" +
            ", topic='" + getTopic() + "'" +
            ", user='" + getUser() + "'" +
            ", timeStamp='" + getTimeStamp() + "'" +
            ", deleted='" + getDeleted() + "'" +
            ", likes='" + getLikes() + "'" +
            "}";
    }

}
