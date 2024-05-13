using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// helper for MapsManager
public class MapMarker{

    // marker size
    public static string SIZE_TINY = "tiny";
    public static string SIZE_MEDIUM = "mid"; 
    public static string SIZE_SMALL = "small";

    // fields
    private string size = "";       // one of {tiny, mid, small}
    private string color = "red";   // hex string of color or one of the predefined colors {black, brown, green, purple, yellow, blue, gray, orange, red, white}.
    private char? label;        // single uppercase char {A-Z, 0-9}
    private string icon = "";       // custom marker icon image URL, 4096 pixels max
    private List<string> locations = new List<string>(); // marker geo-locations in the form of "lat,long"

    public MapMarker(string Size = "", string Color = "", char Label = '\0', string IconURL = "", List<string> Locations = null){
        if (!String.IsNullOrEmpty(Size)){ size = Size; }
        if (!String.IsNullOrEmpty(Color)){ color = Color; }
        if (Label != '\0'){ label = Label; }
        if (!String.IsNullOrEmpty(IconURL)){ icon = IconURL; }
        if (Locations != null){ locations = Locations;}
    }

    // formats the marker properties to the HTTP request param string for MapsManager
    public string Get(){
        string properties = "markers=";

        if (locations.Count == 0){
            // no markers specified, return empty string
            return "";
        }

        if (!String.IsNullOrEmpty(size)){
            properties = properties + "size:" + size + "|";
        }

        if (!String.IsNullOrEmpty(color)){
            properties = properties + "color:" + color + "|";
        }

        if (label != null){
            properties = properties + "label:" + label + "|";
        }

        if (!String.IsNullOrEmpty(icon)){
            properties = properties + "icon:" + icon + "|";
        }

        properties = properties + String.Join('|', locations);

        // TODO: value check before return
        return properties;
    }

    public void AddLocation(string latlong){
        locations.Add(latlong);
    }

    public void AddLocations(List<string> geocoordinates){
        locations.AddRange(geocoordinates);
    }

}   