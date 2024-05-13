using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Firebase;
using Firebase.Auth;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class UserManager : MonoBehaviour
{
    public static UserManager instance;

    // variable
    public Texture2D userProfileTexture;

    void Awake()
    {
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);

        }else if (instance != null){
            Debug.Log("UserManager: instance already exists, destroying object");
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public DBUser GetDBUser(String uid = null){
        /* The uid argument should be the firebase user id */

        Firebase.Auth.FirebaseUser requestor = FirebaseManager.instance.user;
        if (requestor != null) {
            // HTTP request url
            string URL = APIManager.USER_URL;
            if (uid != null){
                URL = URL + "/?uid=" + uid;
            }

            // build request header
            var header = new Dictionary<string, string>(){
                {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
            };

            // call backend API to get recommended topics list for user at location
            Response response = APIManager.Get(URL, header);
            string data = response.Data;

            // process data
            if (!String.IsNullOrEmpty(data)){
                DBUser user = JsonConvert.DeserializeObject<DBUser>(data);
                return user;

            }else{
                Debug.Log("UserManager: GetDBUser returned empty.");
            }
        }

        return null;
    }

    public User GetUser(String uid){
        /* The uid argument should be the firebase user id */

        Firebase.Auth.FirebaseUser requestor = FirebaseManager.instance.user;
        if (requestor != null) {
            // HTTP request url
            string URL = APIManager.GET_FB_USER_URL + "/?uid=" + uid;

            // build request header
            var header = new Dictionary<string, string>(){
                {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
            };

            // call backend API to get recommended topics list for user at location
            Response response = APIManager.Get(URL, header);
            string data = response.Data;

            // process data
            if (!String.IsNullOrEmpty(data)){
                User user = JsonConvert.DeserializeObject<User>(data);
                return user;

            }else{
                Debug.Log("UserManager: GetUser returned empty.");
            }
        }

        return null;
    }


    public IEnumerator AddUser(){
        Firebase.Auth.FirebaseUser user = FirebaseManager.instance.user;
        if (user != null) {
            // set default app preference for this user
            PlayerPrefs.SetInt(UIManager.TEXT_SIZE, UIManager.NORMAL_FONT_SIZE);
            PlayerPrefs.SetInt(UIManager.ENABLE_RECOMMENDATION, Convert.ToInt32(true));
            // PlayerPrefs.SetFloat(UIManager.DISPLAY_DENSITY, 0.5);
            // PlayerPrefs.SetFloat(UIManager.DISPLAY_SPEED, 0.5);
            PlayerPrefs.Save();

            // send HTTP request to backend
            string URL = APIManager.USER_URL;
            Debug.LogFormat("Adding user with token: {0}", FirebaseManager.instance.userToken);
            // build request header
            var header = new Dictionary<string, string>(){
                {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
            };

            yield return APIManager.PostJsonAsync(URL, "", header);

        }
    }

    public IEnumerator UpdateUser(bool new_user, string username, string profilePicPath){
        Firebase.Auth.FirebaseUser user = FirebaseManager.instance.user;
        if (user != null) {

            // check if profile pic is updated
            Uri newURL = user.PhotoUrl;
            if (user.PhotoUrl == null || profilePicPath != user.PhotoUrl.ToString()){
                if (user.PhotoUrl != null){
                    // delete the original profile pic in cloud storage to avoid same user having multiple profile pic with different file extension ('.png', '.jpeg')
                    yield return FirebaseManager.instance.DeleteFile(user.PhotoUrl.ToString().Split('?')[0]);
                }

                // upload the new profile pic to Firebase cloud storage
                yield return FirebaseManager.instance.UploadFile("file://" + profilePicPath, FirebaseManager.PROFILE_PICS_COLLECTION);
                newURL = FirebaseManager.instance.downloadUrl;
            }

            // create a new firebase profile for user
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile {
                DisplayName = username,
                PhotoUrl = newURL
            };

            // update user profile in firebase
            var updateTask = user.UpdateUserProfileAsync(profile);
            yield return new WaitUntil(predicate: () => updateTask.IsCompleted);

            if (updateTask.Exception != null){
                Debug.LogError("UserManager: UpdateUserProfileAsync encountered an error: " + updateTask.Exception);
                if (new_user){
                    GameObject.Find("NewUser_WarningText").GetComponent<TMP_Text>().text = "Encountered an error while updating your profile, please try again.";
                }
            }else{
                Debug.Log("UserManager: User profile updated successfully.");
                if (new_user){
                    GameObject.Find("NewUser_WarningText").GetComponent<TMP_Text>().text = "";
                    NewUserUIController.instance.NewUser_NextButton.gameObject.SetActive(true); // enable button to go to subscriptionUI
                }
            }

        }
    }

    public IEnumerator SetUserEmail(string email){
        Firebase.Auth.FirebaseUser user = FirebaseManager.instance.user;
        if (user != null) {

            var task = user.UpdateEmailAsync(email);
            yield return new WaitUntil(predicate: () => task.IsCompleted);

            if (task.IsCanceled) {
                Debug.LogError("UserManager: UpdateEmailAsync was canceled.");

            }else if (task.IsFaulted) {
                Debug.LogError("UserManager: UpdateEmailAsync encountered an error: " + task.Exception);

            }else{
                Debug.Log("UserManager: User email updated successfully.");
            }
        }
    }

    public void DeleteUser(){
        Firebase.Auth.FirebaseUser user = FirebaseManager.instance.user;
        if (user != null) {
            user.DeleteAsync().ContinueWith(task => {
                if (task.IsCanceled) {
                    Debug.LogError("UserManager: DeleteAsync was canceled.");
                    return;
                }
                if (task.IsFaulted) {
                    Debug.LogError("UserManager: DeleteAsync encountered an error: " + task.Exception);
                    return;
                }

                Debug.Log("UserManager: User deleted successfully.");
            });
        }
    }

    public IEnumerator GetUserProfileTexture(){
        FirebaseUser user = FirebaseManager.instance.user;
        if (user != null){
            System.Uri path = user.PhotoUrl;
            if (path != null){
                Debug.LogFormat("user photo URL: {0}", path.ToString());
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path.ToString()))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success){
                        Debug.LogError(uwr.error);
                    }
                    else{
                        // Get downloaded asset bundle
                        userProfileTexture = DownloadHandlerTexture.GetContent(uwr);
                    }
                }

            }else{
                Debug.Log("User photo URL is null");
                userProfileTexture = Resources.Load<Texture2D>("anonUserIcon");
            }
        }
    }


    public void ValidateUserName(){
        // TODO: validate username format
    }

    public IEnumerator GetUserComments(Action<List<Message>> callback, int count, DateTime timestamp = new DateTime())
    {
        int unixTimestamp = 0;
        if (timestamp != (new DateTime())){
            unixTimestamp = (int)(timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }

        // build the request URL
        string URL = APIManager.MESSAGE_URL
                   + "?start-time=" + unixTimestamp.ToString()
                   + "&count=" + count.ToString()
                   + "&type=0";

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get user posts from backend
        // expect list sorted by timestamp from newest to olders
        yield return APIManager.GetAsync(URL, header);
        string data = APIManager.response.Data;

        // process data
        List<Message> posts = new List<Message>();
        if (!String.IsNullOrEmpty(data)) {
            posts.AddRange(
                JsonConvert.DeserializeObject<List<Message>>(data)
                           .Where(m => !m.IsAR).ToList());
        }
        callback(posts);
    }

    public IEnumerator GetUserPosts(Action<List<Message>> callback, int count, DateTime timestamp = new DateTime())
    {
        int unixTimestamp = 0;
        if (timestamp != (new DateTime())) {
            unixTimestamp = (int)(timestamp.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }

        // build the request URL
        string URL = APIManager.MESSAGE_URL
                   + "?start-time=" + unixTimestamp.ToString()
                   + "&count=" + count.ToString()
                   + "&type=1";

        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get user posts from backend
        // expect list sorted by timestamp from newest to olders
        yield return APIManager.GetAsync(URL, header);
        string data = APIManager.response.Data;

        // process data
        List<Message> posts = new List<Message>();
        if (!String.IsNullOrEmpty(data)) {
            posts.AddRange(
                JsonConvert.DeserializeObject<List<Message>>(data)
                           .Where(m => m.IsAR).ToList());
        }
        callback(posts);
    }

    public List<Location> GetUserLocations()
    {
        // build the request URL
        string URL = APIManager.LOCATION_URL;
        // build request header
        var header = new Dictionary<string, string>(){
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // get user's bound locations from backend
        // expect a list sorted by order added
        Response response = APIManager.Get(URL, header);
        string data = response.Data;

        // process data
        if (!String.IsNullOrEmpty(data)) {
            return JsonConvert.DeserializeObject<List<Location>>(data);
        } else {
            Debug.Log("UserManager: GetUserLocations list is empty.");
            return new List<Location>();
        }
    }

    public bool UpdateUserLocation(Location location)
    {
        // build the request URL
        string URL = APIManager.LOCATION_URL
                    + "?location_id=" + location.ID
                    + "&new_name=" + location.Name;
        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // call backend API to update user location
        Response response = APIManager.PutJson(URL, "", header);

        return response.StatusCode == 200;
    }

    public bool DeleteUserLocation(string location_id)
    {
        // build the request URL
        string URL = APIManager.LOCATION_URL
                    + "?location_id=" + location_id;
        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // call backend API to delete location by its id
        Response response = APIManager.DeleteJson(URL, "", header);

        return response.StatusCode == 200;
    }

    public bool UserIsVerifiedAtLocation()
    {
        Vector3 geoLocation = LocationService.instance.GetCurrentLocation();

        // build the request URL
        string URL = APIManager.VERIFY_USER_URL
                    + "?latitude=" + geoLocation.x.ToString()
                    + "&longitude=" + geoLocation.y.ToString();
        // build request header
        var header = new Dictionary<string, string>() {
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // call backend API to delete message by its id
        Response response = APIManager.Get(URL, header);
        string data = response.Data;

        // process data
        if (!String.IsNullOrEmpty(data)) {
            JObject obj = JObject.Parse(data);
            return obj.SelectToken("verified").Value<bool>();

        } else {
            return false;
        }

    }

    public bool SubscribeAll(List<string> topicIDs)
    {
        // build the request URL
        string URL = APIManager.SUBSCRIPTION_URL;

        // build request header
        var header = new Dictionary<string, string>(){
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // build the json data
        string[] topics = topicIDs.ToArray();
        string jsonString = new JArray(topics).ToString();

        // call backend API to subscribe all topics in the list for the current user
        Response response = APIManager.PostJson(URL, jsonString, header);

        return response.StatusCode == 200;
    }

    public bool UnsubscribeAll(List<string> topicIDs)
    {
        // build the request URL
        string URL = APIManager.SUBSCRIPTION_URL;

        // build request header
        var header = new Dictionary<string, string>(){
            {"Authorization", "Bearer " + FirebaseManager.instance.userToken}
        };

        // build the json data
        string jsonString = "[\"" + String.Join("\",\"", topicIDs.ToArray()) + "\"]";

        // call backend API to subscribe all topics in the list for the current user
        Response response = APIManager.DeleteJson(URL, jsonString, header);

        return response.StatusCode == 200;
    }
}
