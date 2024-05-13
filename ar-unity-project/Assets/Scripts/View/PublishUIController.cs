using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PublishUIController : MonoBehaviour
{
    public static PublishUIController instance;

    // UI components
    public Button BackButton;
    public Button PlaceCommentButton;
    public Button EnableAnchorButton;
    public TMP_Dropdown TopicsDropdown;
    public TMP_InputField CommentInput;
    public TMP_Text WarningText;
    public GameObject OKDialog;
    public GameObject ErrorDialog;
    public Button RetryButton;
    public Button DiscardButton;

    // variables
    private List<Topic> topic_list;
    private List<string> topic_names = new List<string>();
    private string selected_topic;

    private static string DEFAULT_TOPIC = "-- select topic --";

    void Awake()
    {
        if (instance == null){
            instance = this;

        }else if (instance != null){
            Debug.Log("PublishUIController: instance already exists, destroying object");
            Destroy(this);
        }

        CommentInput = GameObject.Find("CommentInput").GetComponent<TMP_InputField>();

        WarningText = GameObject.Find("WarningText").GetComponent<TMP_Text>();

        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.Goto_MainScene);

        PlaceCommentButton = GameObject.Find("PlaceCommentButton").GetComponent<Button>();
        PlaceCommentButton.onClick.AddListener(onClickPublish);

        EnableAnchorButton = GameObject.Find("EnableAnchorButton").GetComponent<Button>();
        EnableAnchorButton.onClick.AddListener(UIManager.instance.Goto_PublishARScene);

        TopicsDropdown = GameObject.Find("TopicsDropdown").GetComponent<TMP_Dropdown>();
        TopicsDropdown.onValueChanged.AddListener(delegate {
            if (TopicsDropdown.value != 0){
                selected_topic = topic_list[TopicsDropdown.value-1].ID;
            }else{
                selected_topic = "";
            }

        });

        OKDialog = GameObject.Find("OKDialog");
        ErrorDialog = GameObject.Find("ErrorDialog");

        RetryButton = GameObject.Find("RetryButton").GetComponent<Button>();
        RetryButton.onClick.AddListener(()=>{
            onClickPublish();
        });

        DiscardButton = GameObject.Find("DiscardButton").GetComponent<Button>();
        DiscardButton.onClick.AddListener(()=>{
            UIManager.instance.Goto_MainScene();
        });
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(getTopicsForDropdown());

        // update text display size according to user preference
        UIManager.UpdateTextSize();

        OKDialog.SetActive(false);
        ErrorDialog.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Auto-fill at dropdown input
    }

    private IEnumerator getTopics(){
        yield return TopicManager.instance.GetTopicsList(callback: (topics)=>{
            topic_list = topics;
            TopicManager.instance.topic_list = topics;
        });
    }

    private IEnumerator getTopicsForDropdown(){

        // wait for all topics
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{StartCoroutine(getTopics());},
            loadingCondition: ()=>{return topic_list == null;},
            errorCondition: ()=>{return false; /* TODO */ },
            timeoutSeconds: 30.0f
        );

        topic_list.Sort((l, r) => String.Compare(l.Name, r.Name)); // sort alphabetically
        foreach (Topic t in topic_list) {
            topic_names.Add(t.Name);
        }

        topic_names.Insert(0, DEFAULT_TOPIC);

        TopicsDropdown.ClearOptions();
        TopicsDropdown.AddOptions(topic_names);
        selected_topic = "";
    }

    private void onClickPublish()
    {
        if (String.IsNullOrEmpty(CommentInput.text)){
            WarningText.text = "Cannot publish empty comment.";
            return;
        }
        else if (String.IsNullOrEmpty(selected_topic)){
            WarningText.text = "Please select a topic from the dropdown.";
            return;
        }
        WarningText.text = "";

        // TODO: validate input text

        // get the device location
        #if UNITY_EDITOR
        double[] coordinates = new double[] {43.471754, -80.523660, 0.0};
        #else
        double[] coordinates = new double[] {
            ARContentManager.instance.latitude, ARContentManager.instance.longitude, ARContentManager.instance.altitude
        };
        #endif

        // update the input message
        InputManager.instance.message = new Message {
            User = FirebaseManager.instance.user.UserId,
            Topic = selected_topic,
            Coordinates = coordinates,
            Color = 0,
            Timestamp = DateTime.UtcNow,
            Content = CommentInput.text,
            IsAR = false,
            Preview = "",
            ImageFormat = "",
            Style = "",
            Scale = 0f
        };


        // publish the comment
        if (InputManager.instance.Publish()) {
            StartCoroutine(waitThenGotoMainScene());
        } else {
            ErrorDialog.SetActive(true);
        }
    }

    private IEnumerator waitThenGotoMainScene()
    {
        ErrorDialog.SetActive(false);
        OKDialog.SetActive(true);
        yield return new WaitForSeconds(1);
        UIManager.instance.Goto_MainScene();
    }
}
