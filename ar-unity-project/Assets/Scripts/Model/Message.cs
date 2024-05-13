using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// comment (pure text) or AR content (text or image)
public class Message
{
    /* this class should be kept identical with backend model class NearMeMessage */
    public string ID { get; set; }
    public string User { get; set; }        // uid of the user who published
    public string Topic { get; set; }       // topic name ==> use Topic object instead?
    public double[] Coordinates { get; set; }   // {lat, lon, alt}

    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Timestamp { get; set; }

    public int Likes { get; set; }
    public string Content { get; set; }     // base64 string for AR image
    public bool IsAR { get; set; }
    public bool IsLiked { get; set; }       // if the current user liked this post

    // below fields are applicable to AR content only
    public float[] Size { get; set; }        // AR image size {width, height}
    public string ImageFormat { get; set; } // AR image texture format
    public string Preview { get; set; }     // AR post preview texture, format=RGBA32, base64 encoded string
    public int Color { get; set; }          // AR text color; see PublishARUIController
    public string Style { get; set; }       // AR style: CLEAR_BG, SOLID_BG, POSTIT, AR_IMAGE
    public float Scale {get; set;}          // AR object scale
    public float[] Rotation {get; set;}     // AR object rotation (quaternion)
}
