using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // mock dependency
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommentHistoryUIController : MonoBehaviour
{
    // prefab
    public GameObject ListItemPrefab;

    // UI components - Base canvas
    public Button BackButton;
    public GameObject Content;
    public GameObject BaseCanvas;

    // UI components - Expanded view canvas
    private GameObject ExpandedViewCanvas;
    private Button DetailsBackButton;

    // Expanded view buttons
    private Button EditButton;
    private Button DoneButton;
    private Button DeleteButton;

    // Expanded view - editable fields
    private TMP_InputField DetailsTextEdit;
    private TMP_InputField DetailsTopicEdit;

    // Expanded view - other fields
    private TMP_Text DetailsLikes;
    private TMP_Text DetailsTimestamp;
    private TMP_Text DetailsLocation;

    public RawImage MapImage;

    // variables
    private List<Message> commentsList; // this one is used to obtain new comments from backend
    private Dictionary<string, Message> commentsDict = new Dictionary<string, Message>();   // this one is used to display on screen
    private string currentExpandedID;
    private bool mapLoaded;
    private Message mapMessage;
    private DateTime nextTime; // starting timestamp for batch fetching comments
    private GameObject dialogObject;
    private bool lastHasUpdate;
    private bool fetchDone = true;

    void Awake(){
        // Base canvas
        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        Content = GameObject.Find("Content");
        // plan: ScrollRect.OnValueChanged
        GameObject.Find("Scroll View").GetComponent<ScrollRect>().onValueChanged.AddListener(
            (Vector2 value) => {
                if (fetchDone && value.y <= 0.05f) {
                    fetchDone = false;
                    Debug.Log("Fetching...");
                    //Debug.Log("Determined scrolled to bottom " + value.ToString());
                    StartCoroutine(renderCommentsList(nextTime));
                }
            }
        );

        BaseCanvas = GameObject.Find("BaseCanvas");

        // Expanded view canvas
        ExpandedViewCanvas = GameObject.Find("ExpandedViewCanvas");

        DetailsBackButton = GameObject.Find("DetailsBackButton").GetComponent<Button>();
        DetailsBackButton.onClick.AddListener(()=>{
            currentExpandedID = "";
            BaseCanvas.SetActive(true);
            ExpandedViewCanvas.SetActive(false);
        });

        DeleteButton = GameObject.Find("DeleteButton").GetComponent<Button>();
        DeleteButton.onClick.AddListener(()=>{
            onClickDelete();
        });
        EditButton = GameObject.Find("EditButton").GetComponent<Button>();
        EditButton.onClick.AddListener(()=>{
            onClickEdit();
        });
        DoneButton = GameObject.Find("DoneButton").GetComponent<Button>();
        DoneButton.onClick.AddListener(()=>{
            StartCoroutine(onClickDone());
        });

        MapImage = GameObject.Find("Map").GetComponent<RawImage>();

        DetailsTextEdit = GameObject.Find("details_text_edit").GetComponent<TMP_InputField>();
        DetailsTopicEdit = GameObject.Find("details_topic_edit").GetComponent<TMP_InputField>();

        DetailsLikes = GameObject.Find("details_likes").GetComponent<TMP_Text>();
        DetailsTimestamp = GameObject.Find("details_timestamp").GetComponent<TMP_Text>();
        DetailsLocation = GameObject.Find("details_location").GetComponent<TMP_Text>();

    }

    // Start is called before the first frame update
    void Start()
    {
        // hide details view
        ExpandedViewCanvas.SetActive(false);

        // clear all if something is there ( ??? )
        ClearAll(Content);

        lastHasUpdate = false;

        // render comments list
        StartCoroutine(renderCommentsList(DateTime.UtcNow));

        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    // Update is called once per frame
    void Update(){}

    private void handleMapDisplay(Texture2D map){
        if (map != null){
            //LoadingIcon.SetActive(false);
            MapImage.texture = map;
            Debug.Log("Map view updated");
        }
    }

    private IEnumerator updateMapView(){
        Debug.Log("Getting map view");
        yield return MapsManager.Get2DMapForMessage(
            post: false,
            center: String.Format("{0},{1}", mapMessage.Coordinates[0], mapMessage.Coordinates[1]),
            width: 500,
            height: 150,
            scale: 2,
            callback: handleMapDisplay
        );
    }

    private IEnumerator getCommentHistory(DateTime startTime, int count){
        // get user's comments
        yield return UserManager.instance.GetUserComments(
            callback: (items)=>{
                commentsList = items;
                fetchDone = true;
            }, 
            count, 
            startTime
        );
    }

    private IEnumerator renderCommentsList(DateTime startTime){
        // wait for fetch to return
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{ StartCoroutine( getCommentHistory(startTime, 15) ); }, // ideally this number literal should be inferred from user's screen size
            loadingCondition: ()=>{ return commentsList == null; },
            errorCondition: ()=>{ return false; /* TODO */ },
            timeoutSeconds: 30.0f
        );

        if (commentsList.Count == 0) {
            lastHasUpdate = false;
        }
        else{
            Debug.LogFormat("Rendering {0} comments", commentsList.Count);
            int currentSize = commentsDict.Count;
            for (int i = 0; i < commentsList.Count; i++) {
                if (!commentsDict.ContainsKey(commentsList[i].ID)) {
                    commentsDict[commentsList[i].ID] = commentsList[i];
                    createListItem(commentsList[i]);
                }
            }
            if (currentSize == commentsDict.Count) {
                // No effective change
                lastHasUpdate = false;
            }
            else{
                lastHasUpdate = true;
            }
            nextTime = commentsList[commentsList.Count - 1].Timestamp;

            // reset to null after we moved them to the dict
            commentsList = null;
        }
    }

    private void ClearAll(GameObject panel) {
        // clear all buttons instantiated
        foreach (Transform button in panel.GetComponent<RectTransform>()) {
            GameObject.Destroy(button.gameObject);
        }
    }

    private void updateListItem(Message item) {
        Transform listItem = Content.transform.Find(item.ID).transform;
        // update content
        TMP_Text comment = listItem.Find("CommentContainer/Comment").gameObject.GetComponent<TMP_Text>();
        comment.text = item.Content;
        comment.name += item.ID;

        // update topic name
        TMP_Text topic = listItem.Find("BottomContainer/TopicContainer/TopicButton/TopicName").gameObject.GetComponent<TMP_Text>();
        topic.text = item.Topic;
        topic.name += item.ID;
        GameObject topicButton = listItem.Find("BottomContainer/TopicContainer/TopicButton").gameObject;
        topicButton.name += item.ID;

        // resize topic button
        float preferredWidth = LayoutUtility.GetPreferredWidth(topic.GetComponent<RectTransform>());
        RectTransform topicNameTransform = topic.GetComponent<RectTransform>();
        topicNameTransform.sizeDelta = new Vector2 (preferredWidth, topicNameTransform.sizeDelta.y);

        RectTransform topicButtonTransform = topicButton.GetComponent<RectTransform>();
        topicButtonTransform.sizeDelta = new Vector2 (preferredWidth + 80, topicButtonTransform.sizeDelta.y);
        topicButtonTransform.localScale = Vector2.one;

        // likes
        TMP_Text likes = listItem.Find("BottomContainer/LikesContainer/Likes").gameObject.GetComponent<TMP_Text>();
        likes.text = item.Likes.ToString();
        likes.name += item.ID;

        // timestamp
        TMP_Text timeStamp = listItem.Find("TopContainer/Timestamp").gameObject.GetComponent<TMP_Text>();
        timeStamp.text = item.Timestamp.ToLocalTime().ToString();
        timeStamp.name += item.ID;
    }

    private void createListItem(Message item) {
        GameObject listItem = (GameObject) Instantiate( ListItemPrefab );
        listItem.name = item.ID;
        
        // set parent
        listItem.GetComponent<RectTransform>().SetParent(Content.GetComponent<RectTransform>());
        listItem.transform.localScale = Vector2.one; // reset prefab scale after set parent to scroll rect

        updateListItem(item);

        Button expandButton = listItem.GetComponent<Button>();
        expandButton.onClick.AddListener(()=>{onClickExpand(item.ID);});
    }

    private void onClickExpand(string id){
        Message message = commentsDict[id];
        currentExpandedID = id;

        // go to expanded view
        BaseCanvas.SetActive(false);
        ExpandedViewCanvas.SetActive(true);

        // update content
        DetailsTextEdit.text = message.Content;
        DetailsTextEdit.interactable = false;

        DetailsTopicEdit.text = message.Topic;
        DetailsTopicEdit.interactable = false;

        mapMessage = message;
        StartCoroutine(updateMapView());

        DetailsLikes.text = message.Likes.ToString();
        DetailsTimestamp.text = message.Timestamp.ToLocalTime().ToString();
        // DetailsLocation.text = Utils.GetRegionFromCoordinates(message.Latitude, message.Longitude);
        DetailsLocation.text = "location";

        // buttons status init
        EditButton.gameObject.SetActive(true);
        DoneButton.gameObject.SetActive(false);
    }

    private void onClickEdit() {
        // allow user to interact with components
        DetailsTextEdit.interactable = true;
        DetailsTopicEdit.interactable = true;

        // hide edit button and expose done button
        EditButton.gameObject.SetActive(false);
        DoneButton.gameObject.SetActive(true);
        
    }

    private IEnumerator onClickDone() {
        if (DetailsTextEdit.text.Equals(commentsDict[currentExpandedID].Content) &&
            DetailsTopicEdit.text.Equals(commentsDict[currentExpandedID].Topic)) {
            Debug.LogFormat("No change was detected");
            // Not moving away as ... no change yet.
            EditButton.gameObject.SetActive(true);
            DoneButton.gameObject.SetActive(false);
        }
        else {
            // record the changes
            commentsDict[currentExpandedID].Content = DetailsTextEdit.text;
            commentsDict[currentExpandedID].Topic = DetailsTopicEdit.text;
            commentsDict[currentExpandedID].Timestamp = DateTime.Now;
            bool editRes = InputManager.instance.Edit(commentsDict[currentExpandedID]);
            // return to list routine
            // update dialog box
            dialogObject = Dialog.Create(name: "EditActionDialog");
            Dialog.Load(
                title: "",
                body: editRes ? "Content Successfully modified!" : "Error! Content not modified"
            );
            
            // wait for 1s
            yield return new WaitForSeconds(1);
            // destroy the rendered dialog box
            Destroy(dialogObject);
            // go back to base canvas
            ExpandedViewCanvas.SetActive(false);
            BaseCanvas.SetActive(true);
            // update the view and move it to top
            updateListItem(commentsDict[currentExpandedID]);
            Content.transform.Find(currentExpandedID).transform.SetAsFirstSibling();
        }
    }

    private void onClickDelete(){
        // popup confirmation dialog box
        dialogObject = Dialog.Create(name: "DeleteActionDialog");
        Dialog.Load(
            title: "Warning",
            body: "Are you sure you want to delete this comment?",
            button1Text: "Yes",
            button2Text: "No",
            button2Action: onClickCancelDelete
        );

        // add action for Yes button
        GameObject.Find("DialogButton1").GetComponent<Button>().onClick.AddListener(()=>{
            StartCoroutine(onClickConfirmDelete(currentExpandedID));
        });
    }

    private IEnumerator onClickConfirmDelete(string id){
        Debug.LogFormat("delete id: {0}", id);
        // call backend API to delete message 
        TopicManager.instance.DeleteMessageByID(id);

        // update dialog box
        Dialog.Load(
            title: "",
            body: "Deleted!"
        );

        // wait for 1s
        yield return new WaitForSeconds(1);

        // destroy the rendered dialog box
        Destroy(dialogObject);

        // go back to base canvas
        ExpandedViewCanvas.SetActive(false);
        BaseCanvas.SetActive(true);

        // delete rendered gameobject for the list item
        Destroy(Content.transform.Find(id).gameObject);
    }

    private void onClickCancelDelete(){
        Destroy(dialogObject);
    }

    // ------------------------ Below are mock data helpers -----------------------//
    // private System.Random rnd = new System.Random();  // random generator
    // private List<Message> mockGetComments(int count) {
    //     List<Message> ret = new List<Message>();
    //     for (int i = 0; i < count; i++) {
    //         Message newItem = new Message();
    //         newItem.ID = "COMMENTID_" + i;
    //         newItem.Topic = "TOPICID_" + i;
    //         newItem.Image = Encoding.ASCII.GetBytes("");
    //         newItem.Content = RandomString(60);
    //         newItem.IsAR = false;
    //         newItem.Latitude = (float) (rnd.NextDouble() * 90.0);
    //         newItem.Altitude = (float) (rnd.NextDouble() * 5000.0);
    //         newItem.Longitude = (float) (rnd.NextDouble() * 360.0 - 180.0);
    //         newItem.Likes = rnd.Next(0, 10000);
    //         newItem.Timestamp = DateTime.Now.AddDays(-30.0);
    //         newItem.Width = (float) (rnd.NextDouble() * 100.0);
    //         newItem.Height = (float) (rnd.NextDouble() * 100.0);
    //         newItem.User = "THISUSER";
    //         newItem.Preview = Encoding.ASCII.GetBytes("");
    //         ret.Add(newItem);
    //     }
    //     return ret;
    // }
    // private string RandomString(int length)
    // {
    //     const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    //     return new string(Enumerable.Repeat(chars, length).Select(s => s[rnd.Next(s.Length)]).ToArray());
    // }
}
