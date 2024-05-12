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
public class NearMeTopic {
    @Getter @Setter private String ID;
    @Getter @Setter private String Name;
    @Getter @Setter private String RegionId;
    @Getter @Setter private long Timestamp;
    @Getter @Setter private String CreatedBy;
    @Getter @Setter private int MessageCount;
    @Getter @Setter private int SubscriptionCount;
}
