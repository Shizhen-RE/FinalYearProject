using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    // variable
    public Message message = new Message();

    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);

        } else if (instance != null) {
            Debug.Log("InputManager instance already exists, destroying object");
            Destroy(this);
        }
    }

    public bool Publish()
    {
        // build the request URL
        string URL = APIManager.MESSAGE_URL;

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // build the json body
        string jsonString = JsonConvert.SerializeObject(message);
        Debug.LogFormat("publish json payload: {0}", jsonString);

        // call backend API to publish comment/AR content
        Response response = APIManager.PostJson(URL, jsonString, header);

        return response.StatusCode == 200;
    }

    public bool Edit(Message msg)
    {
        // build the request URL
        string URL = APIManager.MESSAGE_URL;

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // build the json body
        string jsonString = JsonConvert.SerializeObject(msg);
        Debug.LogFormat("publish json payload: {0}", jsonString);

        // call backend API to modify comment/AR content
        Response response = APIManager.PutJson(URL, jsonString, header);

        return response.StatusCode == 200;
    }
}
