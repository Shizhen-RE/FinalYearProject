using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MapsManager{
    // google maps API
    public static string GOOGLE_MAPS_API = "https://maps.googleapis.com/maps/api/staticmap?";
    public static string GOOGLE_MAPS_API_KEY = "AIzaSyC2Ia4Wq2Zm2K6aAKQ-4fZDm1ogROWDCWQ"; // TODO: this secret should be kept somewhere else

    // image formats
    public static string FORMAT_PNG = "png";
    public static string FORMAT_PNG32 = "png32";
    public static string FORMAT_JPEG = "jpg";
    public static string FORMAT_JPEG_BASELINE = "jpg-baseline";
    public static string FORMAT_GIF = "gif";

    // map types
    public static string TYPE_ROADMAP = "roadmap";
    public static string TYPE_SATELLITE = "satellite";
    public static string TYPE_HYBRID = "hybrid";
    public static string TYPE_TERRAIN = "terrain";

    // max marker count per request
    public static int MAX_MARKER_COUNT = 200;

    /// <summary>
    /// Get 2D real-world map image via Google static Maps API.
    /// </summary>
    /// <param name="center">Geo-coordinates of the center location in the form of "latitude,longitude"</param>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <param name="markers">Markers on the map, create different MapMarker objects for different marker style</param>
    /// <param name="scale">Scale greater than 1 will return more pixels for higher resolution display</param>
    /// <param name="zoom">
    /// Zoom level of the map image, roughly:
    ///     * 1: World
    ///     * 5: Landmass/continent
    ///     * 10: City
    ///     * 15: Streets
    ///     * 20: Buildings
    /// </param>
    /// <param name="style">Map style: https://developers.google.com/maps/documentation/maps-static/styling</param>
    /// <param name="format">Image format: png (default), jpeg, gif</param>
    /// <param name="type">Map type: roadmap (default), satellite, hybrid, terrain</param>
    /// <param name="callback">Callback function to handle the result map texture</param>
    /// We can also specify map style or add paths on map but those are not required for now
    /// <returns>
    /// 2D texture of the map with markers.
    /// </returns>
    public static IEnumerator Get2DMap(
        string center,
        int width,
        int height,
        int? scale,
        int? zoom,
        string format = "",
        string type = "roadmap",
        List<MapMarker> markers = null,
        string other = "",
        Action<Texture2D> callback = null
    )
    {
        // build the request parameters
        var req_params = new Dictionary<string, string>();

        req_params.Add("center", center);
        req_params.Add("size", String.Format("{0}x{1}", width, height));
        req_params.Add("type", type);
        req_params.Add("key", GOOGLE_MAPS_API_KEY);

        if (scale.HasValue) {req_params.Add("scale", scale.ToString());}
        if (zoom.HasValue) {req_params.Add("zoom", zoom.ToString());}
        if (!String.IsNullOrEmpty(format)) {req_params.Add("format", format);}

        string paramString = string.Join("&", req_params.Select(x => x.Key + "=" + x.Value).ToArray());

        if (markers == null){
            markers = new List<MapMarker>();
        }
        paramString = paramString + '&' + string.Join("&", markers.Select(x => x.Get()).ToArray());

        if (!String.IsNullOrEmpty(other)){
            paramString = paramString + '&' + other;
        }

        // build the request url
        string url = GOOGLE_MAPS_API + paramString;

        // the API URL is restricted to 8192 chars max
        if (url.Length > 8192){
            Debug.LogFormat("MapsManager: request URL length is {0}, greater than 8192", url.Length);
        }

        #if UNITY_IOS
        url.Replace("|", "%7C");
        #endif

        // get the static map image
        Debug.LogFormat("MapsManager: requesting {0}", url);

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success){
                Debug.LogError("MapsManager: Failed to get " + url + "\n" + uwr.error);
                callback(null);

            }else{
                // Get downloaded asset bundle
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                callback(texture);
            }
        }
    }

    public static IEnumerator Get2DMapForTopic(
        string topicName,
        string center,
        int width,
        int height,
        int? zoom = null,
        int? scale = null,
        Action<Texture2D> callback = null
    ){
        List<MapMarker> markers = new List<MapMarker>();

        // create marker for the current location (center)
        MapMarker centerMarker = new MapMarker(
            Color: "blue",
            Locations: new List<string>(){center}
        );
        markers.Add(centerMarker);

        // get messages in topic in the nearest geo-mesh(es)
        List<Message> messages = null;
        IEnumerator<List<Message>> request;
        if (String.IsNullOrEmpty(topicName))
            request = TopicManager.instance.GetNearbyMessages(center);
        else
            request = TopicManager.instance.GetMessagesInTopic(topicName, center);
        while (request.MoveNext()) {
            messages = request.Current;
            yield return null;
        }
        messages.AddRange(messages);

        // get the coordinates
        List<string> commentCoordinates = new List<string>();
        List<string> postCoordinates = new List<string>();
        int max = Math.Min(MAX_MARKER_COUNT, messages.Count);
        for (int i = 0; i < max; i++){
            // TODO: might be better to sort the messages by their distance to the center before dropping the excessive
            Message m = messages[i];
            if (m.IsAR){
                postCoordinates.Add(String.Format("{0},{1}", m.Coordinates[0], m.Coordinates[1]));
            }else{
                commentCoordinates.Add(String.Format("{0},{1}", m.Coordinates[0], m.Coordinates[1]));
            }
        }

        // create markers for the AR posts in topic
        MapMarker postMarkers = new MapMarker(
            Size: MapMarker.SIZE_SMALL,
            Color: "red",
            Locations: postCoordinates
        );
        markers.Add(postMarkers);

        // create markers for the comments in topic
        MapMarker commentMarkers = new MapMarker(
            Size: MapMarker.SIZE_TINY,
            Color: "gray",
            Locations: commentCoordinates
        );
        markers.Add(commentMarkers);

        // get the map with markers
        string style = "style=feature:poi|visibility:off";
        yield return Get2DMap(center, width, height, scale, zoom, format: "", type: "roadmap", markers, other: style, callback);
    }

    public static IEnumerator Get2DMapForMessage(
        bool post,
        string center,
        int width,
        int height,
        int? zoom = null,
        int? scale = null,
        Action<Texture2D> callback = null
    ) {
        Debug.LogFormat("Get2DMapForMessage: {0} @ {1}", post ? "Post" : "Comment", center);

        List<MapMarker> markers = new List<MapMarker>();

        // get the coordinates
        List<string> commentCoordinates = new List<string>();
        List<string> postCoordinates = new List<string>();
        if (post){
            postCoordinates.Add(center);
        } else {
            commentCoordinates.Add(center);
        }

        // create markers for the AR posts in topic
        MapMarker postMarker = new MapMarker(
            Size: MapMarker.SIZE_MEDIUM,
            Color: "red",
            Locations: postCoordinates
        );
        markers.Add(postMarker);

        // create markers for the comments in topic
        MapMarker commentMarker = new MapMarker(
            Size: MapMarker.SIZE_MEDIUM,
            Color: "gray",
            Locations: commentCoordinates
        );
        markers.Add(commentMarker);

        // get the map with markers
        string style = "style=feature:poi|visibility:off";
        yield return Get2DMap(center, width, height, scale, zoom, format: "", type: "roadmap", markers, other: style, callback);
    }
}