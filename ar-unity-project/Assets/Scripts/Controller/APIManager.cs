using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager{

    /* NOTE if you are changing this file, also change here resources/APIDefs.json */

    public static string BASE_URL = "http://34.23.7.234:30876/";

    // Free running APIs
    public static string HEALTH_CHECK_URL = BASE_URL + "healthCheck";   // connectivity check
    public static string VERIFY_USER_URL = BASE_URL + "isVerified"; // verify user at location
    public static string GET_TOPIC_LIST_URL = BASE_URL + "getTopicList";    // get all topics in region
    public static string GET_RECOMMENDED_TOPICS_URL = BASE_URL + "getRecommendedTopicList"; // use recommender for top topics
    public static string GET_FB_USER_URL = BASE_URL + "getFBUser"; // get firebase user profile
    public static string ADD_TOPIC_URL = BASE_URL + "addTopic";
    public static string DELETE_TOPIC_URL = BASE_URL + "deleteTopic";
    public static string LIKE_MESSAGE_URL = BASE_URL + "updateLikes";
    //******* DNE in backend *******//
    public static string CREATE_TOPIC_URL = BASE_URL + "createTopic"; // is it better to remove this from frontend?

    // grouped APIs
    public static string NEARME_URL = BASE_URL + "nearme";
    // main service API
    // POST: get comments and posts to display in AR scene
    // GET: get comments and posts based on geomesh and topic in Map view

    public static string LOCATION_URL = BASE_URL + "location";
    // location API
    // POST: add location   ** 0 reference before changing
    // GET: get all location user hold
    // DELETE: removing a location **For the user**
    // PUT: change location name

    public static string MESSAGE_URL = BASE_URL + "message";
    // message here means the message history of a user
    // POST: add message
    // GET: get some comments/posts the user posted, with some param
    // DELETE: delete the message
    // PUT: edit a message of the user

    public static string SUBSCRIPTION_URL = BASE_URL + "subscription";
    // subscription clearly means what user subscribed to, has nothing (much) to do with topic table
    // GET: get user's subscribed topics
    // POST: subscribe
    // DELETE: unsubscribe

    public static string USER_URL = BASE_URL + "user";
    // for **DB** user management
    // GET: get user based on userid (DB)
    // POST: add user
    // DELETE: remove user  ** 0 reference before changing

    public static Response response;

    public static IEnumerator CheckServerConnectivity(Action<Response> callback){
        // create the HTTP GET request from URL
        UnityWebRequest request = UnityWebRequest.Get(HEALTH_CHECK_URL);

        // send request
        yield return request.SendWebRequest();

        // create response
        Response response = new Response{
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };

        // invoke delegate action on the response
        if (callback != null){
            callback(response);
        }
    }

    // HTTP POST request, content-type = multipart/form-data
    public static Response PostForm(string URL, WWWForm form, Dictionary<string, string> header = null)
    {
        // create the HTTP POST request from URL and data form
        UnityWebRequest request = UnityWebRequest.Post(URL, form);
        if (header != null){
            foreach (var h in header){
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        // send request
        request.SendWebRequest();
        // wait for the response
        while (!request.isDone){};

        if (request.result != UnityWebRequest.Result.Success){
            // Debug.LogError(request.error);
        }else{
            // Debug.LogFormat("Post success: {0}", request.responseCode);
        }

        return new Response{
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };
    }

    //HTTP DELETE request, content-type = application/json
    public static Response DeleteJson(string URL, string bodyJsonString, Dictionary<string, string> header = null)
    {
        // create request
        UnityWebRequest request = new UnityWebRequest(URL, "DELETE");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        if (header != null) {
            foreach (var h in header) {
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("HTTP DELETE request:");
        Debug.Log(URL);
        Debug.Log(bodyJsonString);

        // send request
        request.SendWebRequest();
        // wait for the response
        while (!request.isDone) {};

        Debug.Log("HTTP DELETE response");
        Debug.Log(request.responseCode);

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError(request.error);
        } else {
            Debug.Log(request.downloadHandler.text);
        }

        return new Response {
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };
    }

    public static Response PutJson(string URL, string bodyJsonString, Dictionary<string, string> header = null)
    {
        // create request
        UnityWebRequest request = new UnityWebRequest(URL, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        if (header != null) {
            foreach (var h in header) {
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("HTTP PUT request:");
        Debug.Log(URL);
        Debug.Log(bodyJsonString);

        // send request
        request.SendWebRequest();
        // wait for the response
        while (!request.isDone) {};

        Debug.Log("HTTP PUT response");
        Debug.Log(request.responseCode);

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError(request.error);
        } else {
            Debug.Log(request.downloadHandler.text);
        }

        return new Response {
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };
    }

    //HTTP POST request, content-type = application/json
    public static Response PostJson(string URL, string bodyJsonString, Dictionary<string, string> header = null)
    {
        // create request
        UnityWebRequest request = new UnityWebRequest(URL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        if (header != null) {
            foreach (var h in header) {
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        request.SetRequestHeader("Content-Type", "application/json");

        // Debug.Log("HTTP POST request:");
        // Debug.Log(URL);
        // Debug.Log(bodyJsonString);

        // send request
        request.SendWebRequest();
        // wait for the response
        while (!request.isDone) {};

        // Debug.Log("HTTP POST response");
        // Debug.Log(request.responseCode);

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError(request.error);
        } else {
            Debug.Log(request.downloadHandler.text);
        }

        return new Response {
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };
    }

    // HTTP GET request
    public static Response Get(string URL, Dictionary<string, string> header = null)
    {
        // create the HTTP GET request from URL
        UnityWebRequest request = UnityWebRequest.Get(URL);

        if (header != null) {
            foreach (var h in header) {
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        // Debug.Log("HTTP GET request:");
        // Debug.Log(URL);

        // send request
        request.SendWebRequest();
        // wait for the response
        while (!request.isDone) {};

        // Debug.Log("HTTP GET response:");
        // Debug.Log(request.responseCode);

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError(request.error);
        } else {
            Debug.Log(request.downloadHandler.text);
        }

        return new Response {
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };
    }

    // HTTP GET request
    public static IEnumerator GetAsync(string URL, Dictionary<string, string> header = null, Action<Response> callback = null)
    {
        // create the HTTP GET request from URL
        UnityWebRequest request = UnityWebRequest.Get(URL);

        if (header != null){
            foreach (var h in header){
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        // send request
        request.SendWebRequest();
        while (!request.isDone)
            yield return null;

        if (request.result != UnityWebRequest.Result.Success){
            Debug.LogError(request.error);
        }else{
            Debug.LogFormat("Get success: {0}", request.responseCode);
        }

        // create response
        response = new Response{
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };

        // invoke delegate action on the response
        if (callback != null){
            callback(response);
        }
    }

    public static IEnumerator PostJsonAsync(string URL, string bodyJsonString,
                                                      Dictionary<string, string> header = null,
                                                      Action<Response> callback = null)
    {
        // create request
        UnityWebRequest request = new UnityWebRequest(URL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        if (header != null) {
            foreach (var h in header) {
                request.SetRequestHeader(h.Key, h.Value);
            }
        }

        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("HTTP POST request:");
        Debug.Log(URL);
        Debug.Log(bodyJsonString);

        // send request
        yield return request.SendWebRequest();

        // Debug.Log("HTTP POST response");
        // Debug.Log(request.responseCode);

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError(request.error);
        } else {
            Debug.Log(request.downloadHandler.text);
        }

        // create response
        Response response = new Response{
            Data = request.downloadHandler.text,
            StatusCode = (int)request.responseCode,
            Error = request.error
        };

        // invoke delegate action on the response
        if (callback != null){
            callback(response);
        }
    }
}