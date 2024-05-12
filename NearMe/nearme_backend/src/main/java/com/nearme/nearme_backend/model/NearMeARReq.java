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
public class NearMeARReq {
    @Getter @Setter private double[] CurrentPos;
    @Getter @Setter private double[] LastPos;
    @Getter @Setter private int LastTimestamp;
    @Getter @Setter private String[] Topics;
    @Getter @Setter private String Filter;
    @Getter @Setter private int type;
}
