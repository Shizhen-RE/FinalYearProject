using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Location
{
    /* This class should be kept identical to backend model class NearMeLocation */
    public string ID { get; set; }
    public string Name { get; set; }
    public string StreetAddress { get; set; }
    public double[] Coordinates { get; set; }
    public bool Public { get; set; } // true if the location allows public AR content posting
}
