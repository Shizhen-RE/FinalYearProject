using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class Topic
{
    /* This class should be kept identical to backend model class NearMeTopic */
    public string ID { get; set; }
    public string Name { get; set; }
    public string Region { get; set; }

    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime CreatedOn { get; set; }
    
    public string CreatedBy { get; set; }
    public int SubscriptionCount { get; set; }
    public int MessageCount { get; set; }
}
