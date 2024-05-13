using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public class TopicManager : MonoBehaviour
{
    public static TopicManager instance;

    public List<Topic> topic_list;

    void Awake()
    {
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);

        }else if (instance != null){
            Debug.Log("TopicManager instance already exists, destroying object");
            Destroy(this);
        }
    }

    public Response AddTopic(string topicName, string regionName){
        // get auth token for the current user
        string userToken = FirebaseManager.instance.userToken;

        // build the request URL
        string URL = APIManager.ADD_TOPIC_URL
                    + "?name=" + topicName
                    + "&regionName=" + regionName;

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // send request
        return APIManager.Get(URL, header);
    }

    public Response DeleteTopic(string topicID){
        // get auth token for the current user
        string userToken = FirebaseManager.instance.userToken;

        // build the request URL
        string URL = APIManager.DELETE_TOPIC_URL
                    + "?topic=" + topicID;

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // send request
        return APIManager.Get(URL, header);
    }

    public IEnumerator GetUserTopics(Action<List<Topic>> callback)
    {
        // get auth token for the current user
        string userToken = FirebaseManager.instance.userToken;

        // build the request URL
        string URL = APIManager.GET_TOPIC_LIST_URL
                    + "?uid=" + FirebaseManager.instance.user.UserId;

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get topics from backend
        yield return APIManager.GetAsync(URL, header);
        string data = APIManager.response.Data;

        // process data
        List<Topic> topics = new List<Topic>();
        if (!String.IsNullOrEmpty(data)) {
            topics.AddRange(JsonConvert.DeserializeObject<List<Topic>>(data));
        }
        callback(topics);
    }

    public IEnumerator GetTopicsList(Action<List<Topic>> callback)
    {
        // get auth token for the current user
        string userToken = FirebaseManager.instance.userToken;

        // get device location coordinates
        #if UNITY_EDITOR
        Vector2 geoLocation = new Vector2(43.471754f, -80.523660f);
        #else
        Vector2 geoLocation = new Vector2((float)ARContentManager.instance.latitude,
                                          (float)ARContentManager.instance.longitude);
        #endif

        // build the request URL
        string URL = APIManager.GET_TOPIC_LIST_URL
                    + "?latitude=" + geoLocation.x.ToString()
                    + "&longitude=" + geoLocation.y.ToString();

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get topics from backend
        yield return APIManager.GetAsync(URL, header);
        string data = APIManager.response.Data;

        // process data
        List<Topic> topics = new List<Topic>();
        if (!String.IsNullOrEmpty(data)) {
            topics.AddRange(JsonConvert.DeserializeObject<List<Topic>>(data));
        }
        callback(topics);
    }

    public IEnumerator GetRecommendedTopics(Action<List<Topic>> callback){

        // get device location coordinates
        #if UNITY_EDITOR
        Vector2 geoLocation = new Vector2(43.471754f, -80.523660f);
        #else
        Vector2 geoLocation = new Vector2((float)ARContentManager.instance.latitude,
                                          (float)ARContentManager.instance.longitude);
        #endif

        // build the request URL
        string URL = APIManager.GET_RECOMMENDED_TOPICS_URL
                    + "?latitude=" + geoLocation.x.ToString()
                    + "&longitude=" + geoLocation.y.ToString();
        // build request header
        var header = new Dictionary<string, string>(){
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // call backend API to get recommended topics list for user at location
        Debug.LogFormat("Sending {0}", URL);
        yield return APIManager.GetAsync(URL, header);
        string data = APIManager.response.Data;

        // process data
        List<Topic> topics = new List<Topic>();
        if (!String.IsNullOrEmpty(data)) {
            topics.AddRange(JsonConvert.DeserializeObject<List<Topic>>(data));
        }
        callback(topics);
    }

    public IEnumerator GetSubscribedTopics(Action<List<Topic>> callback)
    {
        // TODO: cache subscriptions since ARContentManager calls this frequently
        // TODO: make this async

        // build the request URL
        string URL = APIManager.SUBSCRIPTION_URL;
        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get user subscribed topics from backend
        yield return APIManager.GetAsync(URL, header);
        string data = APIManager.response.Data;

        // process data
        List<Topic> topics = new List<Topic>();
        if (!String.IsNullOrEmpty(data)) {
            topics.AddRange(JsonConvert.DeserializeObject<List<Topic>>(data));
        }
        callback(topics);
    }

    public IEnumerator<List<Message>> GetMessagesInTopic(string topicName, string center){
        // build the request URL
        string[] coordinates = center.Split(',');
        string URL = APIManager.NEARME_URL
                    + "?topic=" + topicName
                    + "&lat=" + coordinates[0]
                    + "&lon=" + coordinates[1];

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get the messages in topic from backend
        string data = null;
        IEnumerator request = APIManager.GetAsync(URL, header, (response) => data = response.Data);
        while (request.MoveNext())
            yield return null;

        // process data
        if (!String.IsNullOrEmpty(data)) {
            yield return JsonConvert.DeserializeObject<List<Message>>(data);
        } else {
            yield return new List<Message>();
        }
    }

    public IEnumerator<List<Message>> GetNearbyMessages(string center){
        // build the request URL
        string[] coordinates = center.Split(',');
        string URL = APIManager.NEARME_URL
                    + "?lat=" + coordinates[0]
                    + "&lon=" + coordinates[1];

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get the messages in topic from backend
        string data = null;
        IEnumerator request = APIManager.GetAsync(URL, header, (response) => data = response.Data);
        while (request.MoveNext())
            yield return null;

        // process data
        if (!String.IsNullOrEmpty(data)) {
            yield return JsonConvert.DeserializeObject<List<Message>>(data);
        } else {
            yield return new List<Message>();
        }
    }

    public bool DeleteMessageByID(string message_id)
    {
        // build the request URL
        string URL = APIManager.MESSAGE_URL
                    + "?message_id=" + message_id;
        // build request header
        var header = new Dictionary<string, string>(){
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // call backend API to delete message by its id
        Response response = APIManager.DeleteJson(URL, "", header);

        return response.StatusCode == 200;
    }

    public bool LikeMessage(string message_id, bool incr = true)
    {
        /* increment or decrement the like of the message by 1 */
        /* default is to increment */

        // build the request URL
        string URL = APIManager.LIKE_MESSAGE_URL
                    + "?message_id=" + message_id;

        if (incr){
            URL += "&add=1";
        }else{
            URL += "&add=-1";
        }

        // build request header
        var header = new Dictionary<string, string>(){
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // call backend API to delete message by its id
        Response response = APIManager.Get(URL, header);

        return response.StatusCode == 200;
    }

}
