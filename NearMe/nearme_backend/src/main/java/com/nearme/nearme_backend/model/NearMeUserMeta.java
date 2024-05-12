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
public class NearMeUserMeta {
    @Getter @Setter private String ID;
    @Getter @Setter private String Name;
    @Getter @Setter private int TotalLikes;
    @Getter @Setter private int PublicationCount;
    @Getter @Setter private int SubscriptionCount;
    @Getter @Setter private int TopicCount;
}
