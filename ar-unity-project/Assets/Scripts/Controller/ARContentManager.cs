using System;
using System.Text;
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
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

class ARContentManager : MonoBehaviour
{
    public static ARContentManager instance;

    /* Prefabs */
    public GameObject CommentPrefab;
    public GameObject ClearBGTextPrefab;
    public GameObject SolidBGTextPrefab;
    public GameObject PostitTextPrefab;
    public GameObject ARImagePrefab;

    /* Configuration */
    static float commentPeriod = 0.5f;  /* seconds */
    static float postPeriod = 0.5f;     /* seconds */
    static float locationPeriod = 1.0f; /* seconds */
    static float postRadius = 110.0f;   /* meters */
    static int maxActiveCommentObjects = 3; /* see MainViewController */
    static int maxActivePostObjects = 100;
    static int maxComments = 20;
    static int maxPosts = 50;

    /* Comment management */
    double commentsOldLat = 0.0;
    double commentsOldLon = 0.0;
    double commentsNewLat = 0.0;
    double commentsNewLon = 0.0;
    bool commentsEnabled = false;
    bool commentsRefreshed = false;
    long commentsTimestamp = 0;
    List<Message> comments = new List<Message>();
    // Dictionary<string, GameObject> commentObjects = new Dictionary<string, GameObject>();
    // GameObject[] activeCommentObjects = new GameObject[maxActiveCommentObjects];
    // Queue<int> commentObjectRows = new Queue<int>(maxActiveCommentObjects);
    Message[] activeComments = new Message[maxActiveCommentObjects]; /* see MainViewController */
    Coroutine commentsRoutine = null;

    int COMMENT_DISPLAY_RADIUS = 10; // 10m comment visibility radius

    /* Post management */
    double postsOldLat = 0.0;
    double postsOldLon = 0.0;
    double postsNewLat = 0.0;
    double postsNewLon = 0.0;
    bool postsRefreshed = false;
    long postsTimestamp = 0;
    List<Message> posts = new List<Message>();
    Dictionary<string, double> postDistances = new Dictionary<string, double>();
    Dictionary<string, GameObject> postObjects = new Dictionary<string, GameObject>();
    GameObject[] activePostObjects = new GameObject[maxActivePostObjects];
    bool reactivatePosts = false;
    Coroutine postsRoutine = null;

    /* Geospatial */
    AREarthManager earthManager;
    ARAnchorManager anchorManager;
    bool geospatialSupport = false;
    bool geospatialTracking = false;

    /* Location */
    GeospatialPose pose;
    public bool tracking { get { return geospatialTracking; } }
    public double latitude { get { return pose.Latitude; } }
    public double longitude { get { return pose.Longitude; } }
    public double altitude { get { return pose.Altitude; } }

    bool recommend = true; // recommendation-based feed

    public bool serverReachable = true;
    public bool serverError = false;

    bool mainScene = true;

    /* See PublishARUIController */
    Color[] colors = new Color[] {
        new Color(1.0f, 1.0f, 1.0f), // white
        new Color(0.0f, 0.0f, 0.0f), // black
        new Color(1.0f, 72.0f/255.0f, 66.0f/255.0f), // red
        new Color(1.0f, 166.0f/255.0f, 0.0f), // orange
        new Color(250.0f/255.0f, 255.0f/255.0f, 0.0f), // yellow
        new Color(89.0f/255.0f, 250.0f/255.0f, 72.0f/255.0f), // green
        new Color(39.0f/255.0f, 185.0f/255.0f, 253.0f/255.0f) // blue
    };

    void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // TODO: don't fetch content unless in AR scene
        } else if (instance != null) {
            Debug.Log("ARContentManager: instance already exists, destroying object");
            Destroy(this);
        }
    }

    IEnumerator Start()
    {
        for (int i = 0; i < maxActiveCommentObjects; i++)
            activeComments[i] = null;

        for (int i = 0; i < maxActivePostObjects; i++)
            activePostObjects[i] = null;

        earthManager = new AREarthManager();
        anchorManager = new ARAnchorManager();
#if UNITY_EDITOR
        geospatialSupport = true;
        geospatialTracking = true;
#else
        bool initializing = true;
        while (initializing) {
            FeatureSupported featureSupported = earthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            switch (featureSupported) {
            case FeatureSupported.Unsupported:
                geospatialSupport = false;
                initializing = false;
                break;
            case FeatureSupported.Supported:
                geospatialSupport = true;
                initializing = false;
                break;
            default:
                yield return null;
                break;
            }
        }

        if (geospatialSupport)
            StartCoroutine(GetLocation());
#endif // UNITY_EDITOR

        commentsRoutine = StartCoroutine(GetComments());
        postsRoutine = StartCoroutine(GetPosts());

        yield break;
    }

    void Update()
    {
        /* Start/stop recommending comments/posts based on user preference */
        recommend = !PlayerPrefs.HasKey(UIManager.ENABLE_RECOMMENDATION) || /* default */
                    PlayerPrefs.GetInt(UIManager.ENABLE_RECOMMENDATION) == 1; /* preference */

        /* Start/stop displaying comments based on user preference */
        commentsEnabled = !PlayerPrefs.HasKey(UIManager.DISPLAY_COMMENTS) || /* default */
                          PlayerPrefs.GetInt(UIManager.DISPLAY_COMMENTS) == 1; /* preference */

        if (UIManager.instance.getCurrentScene() != UIManager.MAIN_SCENE) {
            if (mainScene) {
                StopCoroutine(commentsRoutine);
                commentsRoutine = null;
                StopCoroutine(postsRoutine);
                postsRoutine = null;
                RefreshDisplay();
                mainScene = false;
            }
            return;
        } else if (!mainScene) {
            commentsRoutine = StartCoroutine(GetComments());
            postsRoutine = StartCoroutine(GetPosts());
            mainScene = true;
        }

//         if (commentsEnabled) {
//             /* Deactivate comments that have moved off screen */
//             float cutoff = ((RectTransform)CommentContainer.transform).rect.xMin;
//             for (int i = 0; i < maxActiveCommentObjects; i++) {
//                 GameObject commentObject = activeCommentObjects[i];
//                 if (commentObject != null && commentObject.GetComponent<RectTransform>().rect.xMax > cutoff) {
//                     commentObject.SetActive(false);
//                     activeCommentObjects[i] = null;
//                     commentObjectRows.Enqueue(i);
//                 }
//             }
//
//             /* Activate new comments */
//             int numComments = comments.Count;
//             for (int i = 0; commentObjectRows.Count > 0 && i < numComments; i++) {
//                 GameObject commentObject = activeCommentObjects[i];
//                 if (!commentObject.activeSelf) {
//                     int row = commentObjectRows.Dequeue();
//                     activeCommentObjects[row] = commentObject;
//                     /* TODO: Don't hard-code initial position */
//                     commentObject.transform.position = new Vector3(1600.0f, row * -140.0f, 0.0f);
//                     commentObject.SetActive(true);
//                 }
//             }
//
//         }

        if (reactivatePosts) {
            /* Deactivate old posts */
            for (int i = 0; i < maxActivePostObjects; i++) {
                GameObject postObject = activePostObjects[i];
                if (postObject == null)
                    break;
                // Debug.Log("ARContentManager: deactivating post");
                postObject.SetActive(false);
                activePostObjects[i] = null;
            }

            /* Activate new posts */
            int numPosts = posts.Count;
            for (int i = 0; i < maxActivePostObjects && i < numPosts; i++) {
                string id = posts[i].ID;
                double distance = postDistances[id];
                // Debug.LogFormat("ARContentManager: post ({0}, {1}, {2}); user ({3}, {4}, {5}; distance {6} m)",
                //                 posts[i].Coordinates[0], posts[i].Coordinates[1], posts[i].Coordinates[2],
                //                 pose.Latitude, pose.Longitude, pose.Altitude,
                //                 distance);
                if (distance > postRadius)
                    break;
                // Debug.Log("ARContentManager: activating post");
                GameObject postObject = postObjects[id];
                postObject.SetActive(true);
                activePostObjects[i] = postObject;
            }

            reactivatePosts = false;
        }
    }

    public Message GetComment(int i)
    {
        // get one nearest comment for MainViewUIController at row i
        Message result = null;

        // delete i-th active comment
        if (activeComments[i] != null) {
            comments.Add(activeComments[i]); // add it back to enable looping
            activeComments[i] = null;
        }

        // no comments currently
        if (comments.Count == 0){
            return result;
        }

        // sort the comments from nearest to furthest from current location
        Dictionary<string, double> commentDistances = getCommentDistances();
        sortCommentsByDistance(commentDistances);

        // get a new comment to display in the fetched comments list
        Message nearest = comments[0];
        if (commentDistances[nearest.ID] <= COMMENT_DISPLAY_RADIUS) {
            Debug.LogFormat("ARContentManager: displaying comment {0}", nearest.ID);
            result = nearest;
            activeComments[i] = result;

            // remove the comment from the comments list
            comments.RemoveAt(0);
        }

        return result;
    }

    public void RefreshDisplay()
    {
        Debug.Log("Refresh display.");

        /* Refresh comments */
        commentsRefreshed = true;
        // foreach (GameObject commentObject in commentObjects.Values)
        //     Destroy(commentObject);
        comments = new List<Message>();
        for (int i = 0; i < maxActiveCommentObjects; i++)
            activeComments[i] = null;
        // commentObjects = new Dictionary<string, GameObject>();
        // commentObjectRows = new Queue<int>(maxActiveCommentObjects);
        // for (int i = 0; i < maxActiveCommentObjects; i++) {
        //     activeCommentObjects[i] = null;
        //     commentObjectRows.Enqueue(i);
        // }
        /* Old active comments will be removed when they go off screen */
        commentsOldLat = 0.0;
        commentsOldLon = 0.0;

        /* Refresh posts */
        postsRefreshed = true;
        foreach (GameObject postObject in postObjects.Values)
            Destroy(postObject);
        posts = new List<Message>();
        postDistances = new Dictionary<string, double>();
        postObjects = new Dictionary<string, GameObject>();
        for (int i = 0; i < maxActivePostObjects; i++)
            activePostObjects[i] = null;
        postsOldLat = 0.0;
        postsOldLon = 0.0;
    }

    IEnumerator GetLocation()
    {
        while (true) {
            float timestamp = Time.realtimeSinceStartup + locationPeriod;

            ARSessionState arSessionState = ARSession.state;
            switch (arSessionState) {
            case ARSessionState.SessionInitializing:
            case ARSessionState.SessionTracking:
                break;
            default:
                geospatialTracking = false;
                yield return null;
                continue;
            }

            EarthState earthState = earthManager.EarthState;
            switch (earthState) {
            case EarthState.Enabled:
                break;
            default:
                geospatialTracking = false;
                yield return null;
                continue;
            }

            TrackingState trackingState = earthManager.EarthTrackingState;
            switch (trackingState) {
            case TrackingState.Tracking:
                geospatialTracking = true;
                break;
            default:
                geospatialTracking = false;
                yield return null;
                continue;
            }

            /* TODO: check pose accuracy */
            pose = earthManager.CameraGeospatialPose;

            /* Re-sort posts */
            posts.Sort((x, y) => {
                double xd = postDistances[x.ID];
                double yd = postDistances[y.ID];
                return xd < yd ? -1 : Convert.ToInt32(xd > yd);
            });
            reactivatePosts = true;

            yield return new WaitUntil(() => Time.realtimeSinceStartup >= timestamp);
        }
    }

    IEnumerator GetComments()
    {
        while (true) {
            float timestamp = Time.realtimeSinceStartup + commentPeriod;

            if (!(geospatialTracking && serverReachable && commentsEnabled)) {
                yield return new WaitForSeconds(2.0f);
                continue;
            }

            // get topic id list
            string[] topic_ids = new string[] {};
            yield return get_topic_ids((ids) => { topic_ids = ids; });

            commentsNewLat = pose.Latitude;
            commentsNewLon = pose.Longitude;

            // Build content filter
            int filterCount = 100;
            int filterType = 0;
            if (PlayerPrefs.HasKey(UIManager.DISPLAY_DENSITY)) {
                filterCount = 1 + (int)(99 * PlayerPrefs.GetFloat(UIManager.DISPLAY_DENSITY));
            }
            if (PlayerPrefs.HasKey(UserSubscriptionsController.FILTER)) {
                filterType = PlayerPrefs.GetInt(UserSubscriptionsController.FILTER);
            }

            string filter = filterCount + " " + filterType;
            Debug.LogFormat("ARContentManager: using comments filter '{0}'", filter);
            if (PlayerPrefs.HasKey(UserSubscriptionsController.PREFERED1)) {
                filter += "\n" + PlayerPrefs.GetString(UserSubscriptionsController.PREFERED1);
            }
            if (PlayerPrefs.HasKey(UserSubscriptionsController.PREFERED2)) {
                filter += "\n" + PlayerPrefs.GetString(UserSubscriptionsController.PREFERED2);
            }
            if (PlayerPrefs.HasKey(UserSubscriptionsController.PREFERED3)) {
                filter += "\n" + PlayerPrefs.GetString(UserSubscriptionsController.PREFERED3);
            }

            // Build HTTP POST request
            var header = new Dictionary<string, string>() {
                { "Authorization", "Bearer " + FirebaseManager.instance.userToken }
            };
            string body = new JObject {
                { "CurrentPos", new JArray(new double[2] { commentsNewLat, commentsNewLon }) },
                { "LastPos", new JArray(new double[2] { commentsOldLat, commentsOldLon }) },
                { "LastTimestamp", commentsTimestamp },
                { "Topics", new JArray(topic_ids) },
                { "Filter", "none" },
                { "Type", 0 }
            }.ToString();

            commentsRefreshed = false;

            // Make asynchronous HTTP POST request
            yield return APIManager.PostJsonAsync(APIManager.NEARME_URL, body, header, UpdateComments);

            // wait before next fetch
            yield return new WaitUntil(() => Time.realtimeSinceStartup >= timestamp);
        }
    }

    void UpdateComments(Response response)
    {
        if (response.StatusCode != 200) {
            if (!string.IsNullOrEmpty(response.Error)) {
                Debug.LogError("ARContentManager: error getting comments.");
                serverReachable = false;
            } else {
                Debug.LogFormat("ARContentManager: encountered HTTP {0} getting comments.", response.StatusCode);
                serverError = true;
            }
            return;
        } else if (commentsRefreshed)
            return;

        if (string.IsNullOrEmpty(response.Data))
            return;

        JObject responseObj = JObject.Parse(response.Data);
        JArray messageArr = responseObj["Messages"].Value<JArray>();
        List<Message> messages = JsonConvert.DeserializeObject<List<Message>>(messageArr.ToString());

        commentsTimestamp = responseObj["Timestamp"].Value<long>();

        Debug.LogFormat("ARContentManager: {0} filtered comments", messages.Count);

        /* Create new comments */
        foreach (Message comment in messages) {
            Debug.LogFormat("ARContentManager: adding comment {0}", comment.ID);
            comments.Add(comment);
            // GameObject commentObject = Instantiate(CommentPrefab);
            // commentObject.SetActive(false);
            // commentObject.transform.SetParent(CommentContainer.transform);
            // CommentComponent.AddComponent(gameObject, this, comment);
            // MoverComponent.AddComponent(gameObject, new Vector3(-1.0f * commentSpeed, 0.0f, 0.0f));
            // commentObjects[comment.ID] = commentObject;
        }

        /* Calculate the distance of the comments to the current location and sort */
        Dictionary<string, double> commentDistances = getCommentDistances();
        sortCommentsByDistance(commentDistances);

        /* If there are too many comments, remove the furthest comments */
        while (comments.Count > maxComments) {
            int i = comments.Count - 1;
            Debug.LogFormat("ARContentManager: removing comment {0}", comments[i].ID);
            Message comment = comments[i];
            comments.RemoveAt(i);
        }

        commentsOldLat = commentsNewLat;
        commentsOldLon = commentsNewLon;
    }

    IEnumerator GetPosts()
    {
        while (true) {
            float timestamp = Time.realtimeSinceStartup + postPeriod;

            if (!(geospatialTracking && serverReachable)) {
                yield return new WaitForSeconds(2.0f);
                continue;
            }

            // get topic id list
            string[] topic_ids = new string[] {};
            yield return get_topic_ids((ids) => { topic_ids = ids; });

            postsNewLat = pose.Latitude;
            postsNewLon = pose.Longitude;

            // Build content filter
            int filterCount = 100;
            int filterType = 0;
            if (PlayerPrefs.HasKey(UIManager.DISPLAY_DENSITY)) {
                filterCount = 1 + (int)(99 * PlayerPrefs.GetFloat(UIManager.DISPLAY_DENSITY));
            }
            if (PlayerPrefs.HasKey(UserSubscriptionsController.FILTER)) {
                filterType = PlayerPrefs.GetInt(UserSubscriptionsController.FILTER);
            }

            string filter = filterCount + " " + filterType;
            Debug.LogFormat("ARContentManager: using posts filter '{0}'", filter);
            if (PlayerPrefs.HasKey(UserSubscriptionsController.PREFERED1)) {
                filter += "\n" + PlayerPrefs.GetString(UserSubscriptionsController.PREFERED1);
            }
            if (PlayerPrefs.HasKey(UserSubscriptionsController.PREFERED2)) {
                filter += "\n" + PlayerPrefs.GetString(UserSubscriptionsController.PREFERED2);
            }
            if (PlayerPrefs.HasKey(UserSubscriptionsController.PREFERED3)) {
                filter += "\n" + PlayerPrefs.GetString(UserSubscriptionsController.PREFERED3);
            }

            // Build HTTP POST request
            var header = new Dictionary<string, string>() {
                { "Authorization", "Bearer " + FirebaseManager.instance.userToken }
            };
            string body = new JObject {
                { "CurrentPos", new JArray(new double[2] { postsNewLat, postsNewLon }) },
                { "LastPos", new JArray(new double[2] { postsOldLat, postsOldLon }) },
                { "LastTimestamp", postsTimestamp },
                { "Topics", new JArray(topic_ids) },
                { "Filter", filter },
                { "Type", 1 }
            }.ToString();

            postsRefreshed = false;

            // Make asynchronous HTTP POST request
            yield return APIManager.PostJsonAsync(APIManager.NEARME_URL, body, header, UpdatePosts);

            // wait before next fetch
            yield return new WaitUntil(() => Time.realtimeSinceStartup >= timestamp);
        }
    }

    void UpdatePosts(Response response)
    {
        if (response.StatusCode != 200) {
            if (!string.IsNullOrEmpty(response.Error)) {
                Debug.LogError("ARContentManager: error getting AR contents.");
                serverReachable = false;
            } else {
                Debug.LogFormat("ARContentManager: encountered HTTP {0} getting AR contents.", response.StatusCode);
                serverError = true;
            }
            return;
        } else if (postsRefreshed)
            return;

        serverReachable = true;
        serverError = false;

        if (string.IsNullOrEmpty(response.Data))
            return;

        JObject responseObj = JObject.Parse(response.Data);
        JArray messageArr = responseObj["Messages"].Value<JArray>();
        List<Message> messages = JsonConvert.DeserializeObject<List<Message>>(messageArr.ToString());

        postsTimestamp = responseObj["Timestamp"].Value<long>();

        Debug.LogFormat("ARContentManager: {0} filtered posts", messages.Count);

        /* Create new posts */
        bool added = false;
        foreach (Message post in messages) {
            if (postObjects.ContainsKey(post.ID))
                continue;
            added = true;
            Debug.LogFormat("ARContentManager: adding post {0}", post.ID);
            posts.Add(post);
            Debug.Log("ARContentManager: 1");
            postDistances[post.ID] = Haversine(post.Coordinates[0], post.Coordinates[1],
                                               postsNewLat, postsNewLon);
            Debug.Log("ARContentManager: 2");
            Quaternion rotation = new Quaternion(post.Rotation[0], post.Rotation[1],
                                                 post.Rotation[2], post.Rotation[3]);
            Debug.Log("ARContentManager: 3");
            ARGeospatialAnchor anchor = ARAnchorManagerExtensions.AddAnchor(anchorManager,
                                                                            post.Coordinates[0],
                                                                            post.Coordinates[1],
                                                                            post.Coordinates[2],
                                                                            Quaternion.identity);
            Debug.Log("ARContentManager: 4");
            GameObject postObject = CreatePostObject(anchor, post);
            Debug.Log("ARContentManager: 5");
            postObject.SetActive(false);
            postObjects[post.ID] = postObject;
            Debug.Log("ARContentManager: 6");
        }

        if (!added)
            return;

        /* Re-sort posts */
        posts.Sort((x, y) => {
            double xd = postDistances[x.ID];
            double yd = postDistances[y.ID];
            return xd < yd ? -1 : Convert.ToInt32(xd > yd);
        });
        reactivatePosts = true;
            Debug.Log("ARContentManager: 7");

        /* Remove posts if there are too many */
        while (posts.Count > maxPosts) {
            int i = posts.Count-1;
            Debug.Log("ARContentManager: removing post");
            Message post = posts[i];
            posts.RemoveAt(i);
            postDistances.Remove(post.ID);
            GameObject postObject = postObjects[post.ID];
            int index = Array.IndexOf(activePostObjects, postObject);
            if (index > -1)
                activePostObjects[i] = null;
            Destroy(postObject);
            postObjects.Remove(post.ID);
        }

        postsOldLat = postsNewLat;
        postsOldLon = postsNewLon;
    }

    double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthDiameter = 12.756e6;
        const double asinsqrt1 = Math.PI / 2;
        double lat1rad = Math.PI * lat1 / 180.0;
        double lon1rad = Math.PI * lon1 / 180.0;
        double lat2rad = Math.PI * lat2 / 180.0;
        double lon2rad = Math.PI * lon2 / 180.0;
        double sqrtHavLat = Math.Sin((lat1rad - lat2rad) / 2);
        double sqrtHavLon = Math.Sin((lon1rad - lon2rad) / 2);
        double h = sqrtHavLat*sqrtHavLat + Math.Cos(lat1rad) * Math.Cos(lat2rad) * sqrtHavLon*sqrtHavLon;
        if (h > 1) return earthDiameter * asinsqrt1;
        return earthDiameter * Math.Asin(Math.Sqrt(h)); // distance in meter
    }

    private IEnumerator get_topic_ids(Action<string[]> callback){
        List<Topic> subscribed_topics = new List<Topic>();
        yield return TopicManager.instance.GetSubscribedTopics((topics)=>{subscribed_topics.AddRange(topics);});
        if (recommend){
            Debug.Log("Fecthing topics IDs with recommendation mode enabled.");
            // if recommendation mode is enabled, get contents from recommended topics too
            List<Topic> recommended_topics = new List<Topic>();
            yield return TopicManager.instance.GetRecommendedTopics((topics)=>{recommended_topics.AddRange(topics);});
            subscribed_topics.AddRange(
                recommended_topics
                .Where(x => !subscribed_topics.Any(y => y.ID == x.ID))
                .ToList()
            );
        }

        string[] ids = new string[subscribed_topics.Count];
        for (int i = 0; i < subscribed_topics.Count; i++) {
            ids[i] = subscribed_topics[i].ID;
        }

        callback(ids);
    }

    GameObject CreatePostObject(ARGeospatialAnchor anchor, Message post)
    {
        GameObject postObject = null;
        if (post.Style == "AR_IMAGE") {
            // create AR object
            postObject = Instantiate(ARImagePrefab, anchor.transform);

            Debug.Log("ARContentManager: instantiating image");

            float width = post.Size[0];
            float height = post.Size[1];
            float aspectRatio = width/height;

            // apply image texture
            RawImage image = postObject.transform.Find("Canvas/Content").GetComponent<RawImage>();
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(Convert.FromBase64String(post.Content));
            image.texture = tex;
            // image.texture = Utils.LoadTextureFromBytes(width, height,
            //                                            Convert.FromBase64String(post.Content), post.ImageFormat);

            // adjust image size
            RectTransform rt = image.GetComponent<RectTransform>();
            if (width > height) {
                rt.sizeDelta = new Vector2(rt.rect.width, rt.rect.height/aspectRatio);
            } else {
                rt.sizeDelta = new Vector2(rt.rect.width*aspectRatio, rt.rect.height);
            }
        } else {
            if (post.Style == "CLEAR_BG") {
                Debug.Log("ARContentManager: instantiating clear");
                postObject = Instantiate(ClearBGTextPrefab, anchor.transform);
            } else if (post.Style == "SOLID_BG") {
                Debug.Log("ARContentManager: instantiating solid");
                postObject = Instantiate(SolidBGTextPrefab, anchor.transform);
            } else if (post.Style == "POSTIT") {
                Debug.Log("ARContentManager: instantiating postit");
                postObject = Instantiate(PostitTextPrefab, anchor.transform);
            }

            /* See PublishARUIController */

            ARTextColor arTextColor = (ARTextColor)post.Color;
            Color color = colors[(int)arTextColor];

            /* Set foreground color */
            TextMeshProUGUI foreground = postObject.transform.Find("Canvas/Content").GetComponent<TextMeshProUGUI>();
            if (post.Style == "CLEAR_BG") {
                foreground.color = color;
            } else if (post.Style == "SOLID_BG" || post.Style == "POSTIT") {
                switch (arTextColor) {
                case ARTextColor.White:
                case ARTextColor.Yellow:
                    foreground.color = Color.black;
                    break;
                default:
                    foreground.color = Color.white;
                    break;
                }
            }

            /* Set background color */
            if (post.Style == "SOLID_BG") {
                postObject.transform.Find("Canvas/Background").GetComponent<Image>().color = color;
            } else if (post.Style == "POSTIT") {
                postObject.transform.Find("Canvas/Background").GetComponent<RawImage>().color = color;
            }

            // update text value
            postObject.transform.Find("Canvas/Content").GetComponent<TMP_InputField>().text = post.Content;

            // disable input
            postObject.transform.Find("Canvas/Content").GetComponent<TMP_InputField>().interactable = false;

        }

        postObject.transform.Find("Canvas").GetComponent<Button>().onClick.AddListener(() => {
            Debug.Log("ARContentManager: AR content clicked");
            MainViewController.instance.OnClickAR(post);
        });

        postObject.transform.rotation = new Quaternion(post.Rotation[0], post.Rotation[1],
                                                       post.Rotation[2], post.Rotation[3]);

        // adjust object size
        postObject.transform.Find("Canvas").localScale = new Vector3(post.Scale, post.Scale, 1.0f);

        return postObject;
    }


    private Dictionary<string, double> getCommentDistances(){
        // calculate the distance of each comment location to the current location
        Dictionary<string, double> commentDistances = new Dictionary<string, double>();
        for (int i = 0; i < comments.Count; i++){
            Message comment = comments[i];
            commentDistances[comments[i].ID] = Haversine(comment.Coordinates[0], comment.Coordinates[1],
                                                         pose.Latitude, pose.Longitude);
        }
        return commentDistances;
    }

    private void sortCommentsByDistance(Dictionary<string, double> commentDistances){
        // sort comments from nearest to furthest
        comments.Sort((x, y) => {
            double xd = commentDistances[x.ID];
            double yd = commentDistances[y.ID];
            return xd < yd ? -1 : Convert.ToInt32(xd > yd);
        });
    }
}
