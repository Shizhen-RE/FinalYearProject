using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // mock dependency
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostHistoryUIController : MonoBehaviour
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
    private Button DeleteButton;
    private Button EditButton;
    private Button DoneButton;
    private TMP_Text DetailsText;
    private RawImage DetailsPreview;
    // private TMP_Text DetailsTopic;
    private TMP_InputField DetailsTopicEdit;
    private TMP_Text DetailsLikes;
    private TMP_Text DetailsTimestamp;
    private TMP_Text DetailsLocation;

    public RawImage MapImage;

    // variables
    private List<Message> postsList;
    private Dictionary<string, Message> postsDict = new Dictionary<string, Message>();
    private string currentExpandedID;
    private Message mapMessage;
    private DateTime nextTime; // starting timestamp for batch fetching posts
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
                    StartCoroutine(renderPostsList(nextTime));
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

        DetailsPreview = GameObject.Find("details_preview").GetComponent<RawImage>();
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

        ClearAll(Content);
        lastHasUpdate = false;

        // render posts list
        StartCoroutine(renderPostsList(DateTime.UtcNow));

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
            post: true,
            center: String.Format("{0},{1}", mapMessage.Coordinates[0], mapMessage.Coordinates[1]),
            width: 500,
            height: 150,
            scale: 2,
            callback: handleMapDisplay
        );
    }

    private IEnumerator getPostHistory(DateTime startTime, int count){
        // get user's AR post history
        yield return UserManager.instance.GetUserPosts(
            callback: (items)=>{
                postsList = items;
                fetchDone = true;
            }, 
            count, 
            startTime
        );
    }

    private IEnumerator renderPostsList(DateTime startTime){
        // wait for fetch to return
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{ StartCoroutine( getPostHistory(startTime, 30) ); },    // this number literal ideally should be inferred from user display size
            loadingCondition: ()=>{ return postsList == null; },
            errorCondition: ()=>{ return false; /* TODO */ },
            timeoutSeconds: 30.0f
        );

        if (postsList.Count == 0) {
            lastHasUpdate = false;

        }
        else{
            Debug.LogFormat("Rendering {0} posts", postsList.Count);
            int currentSize = postsDict.Count;
            for (int i = 0; i < postsList.Count; i++) {
                if (!postsDict.ContainsKey(postsList[i].ID)) {
                    postsDict[postsList[i].ID] = postsList[i];
                    createListItem(postsList[i]);
                }
            }
            if (currentSize == postsDict.Count()) {
                // No effective change
                lastHasUpdate = false;
            }
            else {
                lastHasUpdate = true;
            }

            nextTime = postsList[postsList.Count - 1].Timestamp;
            postsList = null;
        }
    }

    private void ClearAll(GameObject panel) {
        // clear all buttons instantiated
        foreach (Transform button in panel.GetComponent<RectTransform>()) {
            GameObject.Destroy(button.gameObject);
        }
    }

    private void createListItem(Message item){
        Debug.LogFormat("Instantiating {0}", item.ID);
        GameObject listItem = (GameObject) Instantiate( ListItemPrefab );
        listItem.name = item.ID;

        // set parent
        listItem.GetComponent<RectTransform>().SetParent(Content.GetComponent<RectTransform>());
        listItem.transform.localScale = Vector2.one; // reset prefab scale after set parent to scroll rect

        updateListItem(item);

        Button expandButton = listItem.GetComponent<Button>();
        expandButton.onClick.AddListener(()=>{onClickExpand(item.ID);});
    }

    private void updateListItem(Message item){
        Transform listItem = Content.transform.Find(item.ID).transform;
        
        // update list item
        RawImage preview = listItem.GetComponent<RawImage>();

        Texture2D tex = new Texture2D(2, 2); // dimension will be replaced by load image
        if (!String.IsNullOrEmpty(item.Preview)){
            tex.LoadImage(Convert.FromBase64String(item.Preview));
        }
        preview.texture = tex;
        //preview.texture = Utils.LoadTextureFromBytes(Convert.FromBase64String(item.Preview), "RGBA32");
    }

    private void onClickExpand(string id){
        Message message = postsDict[id];
        currentExpandedID = id;

        // go to expanded view
        BaseCanvas.SetActive(false);
        ExpandedViewCanvas.SetActive(true);

        // update content
        Texture2D tex = new Texture2D(2, 2); // dimension will be replaced by load image
        if (!String.IsNullOrEmpty(message.Preview)){
            tex.LoadImage(Convert.FromBase64String(message.Preview));
        }
        DetailsPreview.texture = tex;

        mapMessage = message;
        StartCoroutine(updateMapView());

        // DetailsTopic.text = message.Topic;
        DetailsTopicEdit.text = message.Topic;
        DetailsLikes.text = message.Likes.ToString();
        DetailsTimestamp.text = message.Timestamp.ToLocalTime().ToString();
        DetailsLocation.text = Utils.GetRegionFromCoordinates(message.Coordinates[0], message.Coordinates[1]);

        // buttons status init
        EditButton.gameObject.SetActive(true);
        DoneButton.gameObject.SetActive(false);
    }

    private void onClickEdit() {
        // allow user to interact with components
        DetailsTopicEdit.interactable = true;

        // hide edit button and expose done button
        EditButton.gameObject.SetActive(false);
        DoneButton.gameObject.SetActive(true);
    }

    private IEnumerator onClickDone() {
        if (DetailsTopicEdit.text.Equals(postsDict[currentExpandedID].Topic)) {
            Debug.LogFormat("No change was detected");
            // Not moving away as ... no change yet.
            EditButton.gameObject.SetActive(true);
            DoneButton.gameObject.SetActive(false);
        }
        else {
            // record the changes
            postsDict[currentExpandedID].Topic = DetailsTopicEdit.text;
            postsDict[currentExpandedID].Timestamp = DateTime.Now;
            bool editRes = InputManager.instance.Edit(postsDict[currentExpandedID]);
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
            if (editRes) {
                updateListItem(postsDict[currentExpandedID]);
                Content.transform.Find(postsDict[currentExpandedID].ID).transform.SetAsFirstSibling();
            }
        }
    }

    private void onClickDelete(){
        // popup confirmation dialog box
        dialogObject = Dialog.Create(name: "DeleteActionDialog");
        Dialog.Load(
            title: "Warning",
            body: "Are you sure you want to delete this post?",
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
    // private List<Message> mockGetPosts(int count) {
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
