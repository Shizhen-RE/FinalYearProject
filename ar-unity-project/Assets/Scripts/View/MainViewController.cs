using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Firebase;
using Firebase.Auth;

public class MainViewController : MonoBehaviour
{
    public static MainViewController instance;

    // prefab
    public GameObject CommentPrefab;

    // swipe detector
    private SwipeDetector swipeDetector;

    // UI components - base canvas
    public RawImage AvatarImage;
    public Button AvatarButton;
    public Button MoreButton;
    public Button LessButton;
    public Button SettingsButton;
    public Button SearchButton;
    public Button BindButton;
    public Button RefreshButton;
    public Button AddButton;
    public GameObject ButtonsDescriptions;

    // UI components - comments canvas
    public GameObject CommentsContainer;

    // UI components - details canvas
    public GameObject DetailsPopup;
    public RawImage PublisherAvatar;
    public TMP_Text PublisherName;
    public TMP_Text PublishDate;
    public TMP_Text DetailsText;
    public TMP_Text DetailsCommentTopic;
    public TMP_Text Likes;
    public GameObject LikeButton;
    public Button CloseButton;
    public Button TopicButton;

    // UI component - map canvas
    public GameObject MapCanvas;
    public RawImage MapImage;
    public GameObject LoadingIcon;

    // variables
    private int COMMENT_START_POSITION = Screen.width;
    private int MAX_COMMENTS = 3;
    private List<int> commentPositions;
    private int totalRenderedComments = 0;
    private Message expandedComment;
    private string mapTopicName;

    private GameObject alertObject;

    // alert messages
    private static string CONNECTION_ERR_MSG = "Failed to connect to server, please check your internet connection.";
    private static string SERVER_ERR_MSG = "Internal server error, please retry after a few moments.";
    private static string GEO_ERR_MSG = "Failed to detect your geo location, please check your internet connection.";

    void Awake(){

        if (instance == null){
            instance = this;

        }else if (instance != null){
            Debug.Log("MainViewController: instance already exists, destroying object");
            Destroy(this);
        }


        AvatarImage = GameObject.Find("ProfilePic").GetComponent<RawImage>();

        AvatarButton = GameObject.Find("ProfilePic").GetComponent<Button>();
        AvatarButton.onClick.AddListener(UIManager.instance.Goto_UserHomepage);

        MoreButton = GameObject.Find("MoreButton").GetComponent<Button>();
        MoreButton.onClick.AddListener(expand_buttons);

        if (LessButton == null){
        LessButton = GameObject.Find("LessButton").GetComponent<Button>();
        }
        LessButton.onClick.AddListener(collapse_buttons);
        LessButton.gameObject.SetActive(false);

        SettingsButton = GameObject.Find("SettingsButton").GetComponent<Button>();
        SettingsButton.onClick.AddListener(UIManager.instance.Goto_SettingsScene);

        SearchButton = GameObject.Find("SearchButton").GetComponent<Button>();
        SearchButton.onClick.AddListener(UIManager.instance.Goto_SubscriptionScene);

        BindButton = GameObject.Find("BindButton").GetComponent<Button>();
        BindButton.onClick.AddListener(UIManager.instance.Goto_BindLocationScene);

        RefreshButton = GameObject.Find("RefreshButton").GetComponent<Button>();
        RefreshButton.onClick.AddListener(refresh_display);

        AddButton = GameObject.Find("AddButton").GetComponent<Button>();
        AddButton.onClick.AddListener(UIManager.instance.Goto_PublishScene);

        ButtonsDescriptions = GameObject.Find("Buttons&Descriptions");

        CommentsContainer = GameObject.Find("CommentsContainer");

        DetailsPopup = GameObject.Find("Details");
        PublisherAvatar = GameObject.Find("PublisherAvatar").GetComponent<RawImage>();
        PublisherName = GameObject.Find("PublisherName").GetComponent<TMP_Text>();
        PublishDate = GameObject.Find("PublishDate").GetComponent<TMP_Text>();
        DetailsText = GameObject.Find("DetailsText").GetComponent<TMP_Text>();
        DetailsCommentTopic = GameObject.Find("TopicName").GetComponent<TMP_Text>();

        Likes = GameObject.Find("Likes").GetComponent<TMP_Text>();
        LikeButton = GameObject.Find("LikeButton");
        LikeButton.GetComponent<Button>().onClick.AddListener(onClickLike);
        LikeButton.GetComponent<Image>().color = UIManager.LIGHT_GREY;

        CloseButton = GameObject.Find("CloseButton").GetComponent<Button>();
        CloseButton.onClick.AddListener(onClickClose);

        TopicButton = GameObject.Find("TopicButton").GetComponent<Button>();
        TopicButton.onClick.AddListener(onClickTopic);

        MapCanvas = GameObject.Find("MapCanvas");
        MapImage = GameObject.Find("MapView").GetComponent<RawImage>();
        LoadingIcon = GameObject.Find("LoadingIcon");

        swipeDetector = GameObject.Find("SwipeDetector").GetComponent<SwipeDetector>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // get user profile pic from firebase
        if (FirebaseManager.instance.user != null){
            StartCoroutine(updateAvatarImage());
        }

        // update text display size according to user preference
        UIManager.UpdateTextSize();

        // deactivate views
        ButtonsDescriptions.SetActive(false);
        DetailsPopup.SetActive(false);
        MapCanvas.SetActive(false);

        // set initial position for comments
        commentPositions = Enumerable.Range(0, MAX_COMMENTS).ToList();

        // configure swipe detector for map view
        swipeDetector.Active = true;
        swipeDetector.OnSwipeDown.AddListener(closeMapView);
        swipeDetector.OnSwipeUp.AddListener(showMapView);

        StartCoroutine(updateMapView());
    }

    // Update is called once per frame
    // Default 30 frames per second
    void Update()
    {
        // check server connection and geo tracking
        #if !UNITY_EDITOR
        checkConnectivity();
        #endif

        // get comments from ARContentManager
        if (totalRenderedComments < MAX_COMMENTS) {
            Message comment = null;
            var random = new System.Random().Next(commentPositions.Count);
            int commentRow = commentPositions[random];
            if ((comment = ARContentManager.instance.GetComment(commentRow)) != null) {
                commentPositions.RemoveAt(random);
                StartCoroutine(renderComment(comment, commentRow));
            }
        }
    }

    private IEnumerator updateMapView(){
        while(true){
            if (ARContentManager.instance.tracking && MapCanvas.activeSelf){
                Debug.LogFormat("MainViewController: getting map view for '{0}'", mapTopicName);
                yield return MapsManager.Get2DMapForTopic(
                    topicName: mapTopicName, // if empty string, fetch all topics
                    center: String.Format("{0},{1}", ARContentManager.instance.latitude, ARContentManager.instance.longitude),
                    width: 510,
                    height: 450,
                    scale: 2,
                    callback: handleMapDisplay
                );

                // wait for 3s before refetch
                yield return new WaitForSeconds(3); // TODO: validate this rate
            }else{
                // wait for 1s before rechecking
                yield return new WaitForSeconds(1);
            }
        }
    }

    private IEnumerator renderComment(Message comment, int commentRow){
        Debug.LogFormat("rendering comment {0}", comment.ID);

        GameObject item = (GameObject) Instantiate( CommentPrefab );
        item.name = comment.ID;

        // updated comment text and font size
        TMP_Text commentText = GameObject.Find("CommentText").GetComponent<TMP_Text>();
        commentText.text = comment.Content;
        commentText.gameObject.name += comment.ID;
        commentText.fontSize = (commentText.fontSize/UIManager.NORMAL_FONT_SIZE) * PlayerPrefs.GetInt(UIManager.TEXT_SIZE);

        // get textbox width and height
        RectTransform commentTextTransform = commentText.GetComponent<RectTransform>();
        float preferredWidth = LayoutUtility.GetPreferredWidth(commentTextTransform );
        float preferredHeight = LayoutUtility.GetPreferredHeight(commentTextTransform );
        Vector2 textboxSize = new Vector2(preferredWidth, 100);

        // get textbox position
        Vector3 textboxPosition = new Vector3(COMMENT_START_POSITION, commentRow * -140, 0);

        // adjust comment size and position
        RectTransform commentTransform = item.GetComponent<RectTransform>();
        commentTransform.SetParent(CommentsContainer.GetComponent<RectTransform>());
        commentTransform.sizeDelta = new Vector2(textboxSize.x+80, textboxSize.y+40);
        commentTransform.anchoredPosition = textboxPosition;
        commentTransform.localScale = Vector2.one;

        GameObject background = GameObject.Find("CommentBackground");
        background.name += comment.ID;
        background.GetComponent<RectTransform>().sizeDelta = new Vector2(textboxSize.x+80, textboxSize.y+40);
        background.GetComponent<RectTransform>().localScale = Vector2.one;

        commentTextTransform.sizeDelta = textboxSize;
        commentTextTransform.localScale = Vector2.one;

        // de-highlight
        Color c = item.GetComponent<Image>().color;
        c.a = 0;
        item.GetComponent<Image>().color = c;

        // set action
        Button commentButton = item.GetComponent<Button>();
        commentButton.onClick.AddListener(()=>{onClickComment(comment);});

        item.AddComponent<Mover>();

        totalRenderedComments += 1;

        float stopPosition = - textboxSize.x;

        // wait for comment to disappear
        while(item.transform.position.x - stopPosition > 0){
            yield return new WaitForSeconds(1);
        }

        // destroy rendered comment
        Destroy(item);
        commentPositions.Add(commentRow);
        totalRenderedComments -= 1;
    }


    public void OnClickAR(Message post){
        Debug.Log("AR content clicked");
        showDetails(post);
    }

    private void onClickComment(Message comment){
        Debug.Log("comment clicked");

        // find comment object
        GameObject obj = GameObject.Find(comment.ID);
        expandedComment = comment;

        // pause comment
        obj.GetComponent<Mover>().Pause = true;

        // highlight comment
        Color c = obj.GetComponent<Image>().color;
        c.a = 1;
        obj.GetComponent<Image>().color = c;

        // disable other comment buttons
        GameObject[] commentsList = GameObject.FindGameObjectsWithTag("Comment");
        foreach (GameObject i in commentsList) {
            i.GetComponent<Button>().interactable = false;
        }

        // bring up the details pop up
        showDetails(comment);
    }

    private void showDetails(Message m){

        // close the map view if it was opened
        closeMapView();

        DetailsPopup.SetActive(true);
        if (!m.IsAR){
            DetailsText.text = m.Content;
        }else{
            DetailsText.text = "";
        }
        DetailsCommentTopic.text = m.Topic;
        PublishDate.text = m.Timestamp.ToLocalTime().ToString();
        Likes.text = m.Likes.ToString();
        if (m.IsLiked){
            LikeButton.GetComponent<Image>().color = UIManager.RED;
        }else{
            LikeButton.GetComponent<Image>().color = UIManager.LIGHT_GREY;
        }

        StartCoroutine(getPublisherInfo(m.User));
    }

    private IEnumerator getPublisherInfo(string uid){

        User user = UserManager.instance.GetUser(uid);

        if (user != null){

            PublisherName.text = user.displayName;

            if (user.photoUrl != null){
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(user.photoUrl))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success){
                        Debug.LogError(uwr.error);
                    }
                    else{
                        // Get downloaded asset bundle
                        PublisherAvatar.texture = DownloadHandlerTexture.GetContent(uwr);
                    }
                }
            }else{
                PublisherAvatar.texture = Resources.Load<Texture2D>("anonUserIcon");
            }
        }else{
            PublisherName.text = "Unknown";
            PublisherAvatar.texture = Resources.Load<Texture2D>("anonUserIcon");
        }
    }

    private void onClickClose(){
        // check if like button is highlighted
        bool like = (LikeButton.GetComponent<Image>().color == UIManager.RED);

        // close details window
        DetailsPopup.SetActive(false);

        // find comment object
        GameObject obj = GameObject.Find(expandedComment.ID);

        // de-highlight comment
        Color c = obj.GetComponent<Image>().color;
        c.a = 0;
        obj.GetComponent<Image>().color = c;

        // continue moving
        obj.GetComponent<Mover>().Pause = false;

        // re-enable all the comment buttons
        GameObject[] commentsList = GameObject.FindGameObjectsWithTag("Comment");
        foreach (GameObject i in commentsList) {
            i.GetComponent<Button>().interactable = true;
        }

        // call backend to update likes for the current comment
        if (expandedComment.IsLiked && !like){
            // decrement like
            TopicManager.instance.LikeMessage(expandedComment.ID, false);
        }else if (!expandedComment.IsLiked && like){
            // increment like
            TopicManager.instance.LikeMessage(expandedComment.ID, true);
        }
        // update the cached comment like status
        expandedComment.IsLiked = like;
    }

    private void onClickLike(){
        // toggle like button color
        if (LikeButton.GetComponent<Image>().color == UIManager.LIGHT_GREY){
            LikeButton.GetComponent<Image>().color = UIManager.RED;
            expandedComment.Likes += 1;
            Likes.text = expandedComment.Likes.ToString();
        }else{
            LikeButton.GetComponent<Image>().color = UIManager.LIGHT_GREY;
            expandedComment.Likes -= 1;
            Likes.text = expandedComment.Likes.ToString();
        }
    }

    private void onClickTopic(){
        // display map view
        mapTopicName = expandedComment.Topic;
        MapCanvas.SetActive(true);
        LoadingIcon.SetActive(true);
    }

    private void closeMapView(){
        // close the map view
        MapCanvas.SetActive(false);
    }

    private void showMapView(){
        // dispaly map view for all topics nearby
        MapCanvas.SetActive(true);
        LoadingIcon.SetActive(true);
        mapTopicName = "";
    }

    private void handleMapDisplay(Texture2D map){
        if (map != null){
            LoadingIcon.SetActive(false);
            MapImage.texture = map;
        }
    }

    private void expand_buttons(){
        MoreButton.gameObject.SetActive(false);
        LessButton.gameObject.SetActive(true);
        ButtonsDescriptions.SetActive(true);
    }

    private void collapse_buttons(){
        MoreButton.gameObject.SetActive(true);
        LessButton.gameObject.SetActive(false);
        ButtonsDescriptions.SetActive(false);
    }

    private void refresh_display(){
        ARContentManager.instance.RefreshDisplay();
    }

    private IEnumerator updateAvatarImage(){
        yield return UserManager.instance.GetUserProfileTexture();
        AvatarImage.texture = UserManager.instance.userProfileTexture;
    }

    private void checkConnectivity(){
        if (!ARContentManager.instance.tracking ||
            !ARContentManager.instance.serverReachable ||
            ARContentManager.instance.serverError
            )
        {
            // pop-up UI alert if not already there
            if (!GameObject.Find("alert")){
                // determine the error message
                string alertMsg = "Unknown error";
                if (!ARContentManager.instance.serverReachable){
                    alertMsg = CONNECTION_ERR_MSG;
                }else if(!ARContentManager.instance.tracking){
                    alertMsg = GEO_ERR_MSG;
                }else if (ARContentManager.instance.serverError){
                    alertMsg = SERVER_ERR_MSG;
                }

                // render the alert dialog box
                alertObject = Dialog.Create(name: "alert");
                Dialog.Load(
                    title: "Error",
                    body: alertMsg,
                    button1Text: "Retry",
                    button1Action: onClickRetryConnect
                );
            }
        }
    }

    private void onClickRetryConnect(){
        Debug.Log("Retry reaching cloud server");
        StartCoroutine(APIManager.CheckServerConnectivity(handleConnectivityResult));
    }

    private void handleConnectivityResult(Response response){
        if (ARContentManager.instance.tracking && response.StatusCode == 200){
            // OK response, server is reachable
            ARContentManager.instance.serverReachable = true;
            ARContentManager.instance.serverError = false;

            // destroy the rendered alert
            Destroy(alertObject);

        }else if (!ARContentManager.instance.tracking){
            // OK response, server is reachable
            ARContentManager.instance.serverReachable = true;
            ARContentManager.instance.serverError = false;

            // but geospatial tracking is not available so don't destroy alert yet
            Dialog.SetBody(GEO_ERR_MSG);

        }else if(!string.IsNullOrEmpty(response.Error)){
            // server is not reachable
            ARContentManager.instance.serverReachable = false;
            ARContentManager.instance.serverError = true;

            Dialog.SetBody(CONNECTION_ERR_MSG);

        }else{
            // server is reachable but got some error
            ARContentManager.instance.serverReachable = true;
            ARContentManager.instance.serverError = true;

            Dialog.SetBody(SERVER_ERR_MSG);
        }
    }
}
