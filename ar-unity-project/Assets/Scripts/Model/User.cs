using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User
{
    /* this class is to parse Firebase UserRecord received from backend getUser request */
    public string uid { get; set; }  // Firebase uid
    public string displayName { get; set; }
    public string email { get; set; }
    public string photoUrl { get; set; }
    public bool disabled { get; set; }
}
