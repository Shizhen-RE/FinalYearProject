using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DBUser
{
    /* this class is to parse user mata data received from backend db */
    /* This class should be kept identical to backend model class NearMeUserMeta */
    public string ID { get; set; }  // Firebase uid
    public string Name { get; set; }
    public string TotalLikes { get; set; }
    
    public int PublicationCount { get; set; }
    public int SubscriptionCount { get; set; }
    public int TopicCount { get; set; }
}
