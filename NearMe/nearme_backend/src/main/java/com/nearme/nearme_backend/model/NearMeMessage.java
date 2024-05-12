package com.nearme.nearme_backend.model;

import com.fasterxml.jackson.databind.PropertyNamingStrategies;
import com.fasterxml.jackson.databind.annotation.JsonNaming;

import lombok.AllArgsConstructor;
import lombok.NoArgsConstructor;
import lombok.Getter;
import lombok.Setter;

@NoArgsConstructor
@AllArgsConstructor
@JsonNaming(PropertyNamingStrategies.UpperCamelCaseStrategy.class)
public class NearMeMessage {
    @Getter @Setter private String ID;
    @Getter @Setter private String User;
    @Getter @Setter private String Topic;
    @Getter @Setter private double[] Coordinates;
    @Getter @Setter private long Timestamp;
    @Getter @Setter private int Likes;
    @Getter @Setter private boolean isLiked;
    @Getter @Setter private String Content; // base64 if image
    @Getter @Setter private boolean IsAR;
    @Getter @Setter private float[] Size;
    @Getter @Setter private String ImageFormat;
    @Getter @Setter private String Preview; // also, base64 if image, nothing otherwise
    @Getter @Setter private int Color;
    @Getter @Setter private String Style;
    @Getter @Setter private float Scale;
    @Getter @Setter private float[] Rotation;
}
