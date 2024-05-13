using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserTopicsUIController : MonoBehaviour
{
    // prefab
    public GameObject ListItemPrefab;

    // UI components - base canvas
    private Button BackButton;
    private Button CreateButton;
    private GameObject Content;

    // UI components - expanded view canvas
    private GameObject ExpandedViewCanvas;
    private Button DetailsBackButton;
    private Button DeleteButton;
    private TMP_Text DetailsTopicName;
    private TMP_Text DetailsRegionName;
    private TMP_Text DetailsCreationDate;
    private TMP_Text DetailsSubscriptionsCount;
    private TMP_Text DetailsMessagesCount;
    private Toggle AllowPostingToggle;

    // UI components - create new canvas
    private GameObject CreateNewCanvas;
    private TMP_InputField NewTopicName;
    private TMP_Text NewRegionName;
    private TMP_Text CreateNewWarnigText;
    private Button CreateNewTopicButton;
    private Button CancelCreateButton;

    // variables
    private List<Topic> topicsList = null; // tuple of (topic_name, message_count)
    private List<string> deletedTopics = new List<string>(); // list of topic IDs to be deleted
    private int currentExpandedID;
    private GameObject dialogObject;

    void Awake()
    {
        // base canvas
        BackButton = GameObject.Find("BackButton").GetComponent<Button>();
        BackButton.onClick.AddListener(UIManager.instance.GoBack);

        CreateButton = GameObject.Find("CreateButton").GetComponent<Button>();
        CreateButton.onClick.AddListener(onClickCreate);

        Content = GameObject.Find("Content");

        // expanded view canvas
        ExpandedViewCanvas = GameObject.Find("ExpandedViewCanvas");

        DetailsBackButton = GameObject.Find("DetailsBackButton").GetComponent<Button>();
        DetailsBackButton.onClick.AddListener(() => {
            currentExpandedID = -1;
            ExpandedViewCanvas.SetActive(false);
        });

        DeleteButton = GameObject.Find("DeleteButton").GetComponent<Button>();
        DeleteButton.onClick.AddListener(onClickDelete);

        DetailsTopicName = GameObject.Find("details_name").GetComponent<TMP_Text>();
        DetailsRegionName = GameObject.Find("details_region").GetComponent<TMP_Text>();
        DetailsCreationDate = GameObject.Find("details_creationDate").GetComponent<TMP_Text>();
        DetailsSubscriptionsCount = GameObject.Find("details_subscriptions").GetComponent<TMP_Text>();
        DetailsMessagesCount = GameObject.Find("details_messages").GetComponent<TMP_Text>();

        AllowPostingToggle = GameObject.Find("AllowPostingToggle").GetComponent<Toggle>();

        // create new view
        CreateNewCanvas = GameObject.Find("CreateNewCanvas");
        NewTopicName = GameObject.Find("NewTopicName").GetComponent<TMP_InputField>();
        NewRegionName = GameObject.Find("new_region").GetComponent<TMP_Text>();
        CreateNewWarnigText = GameObject.Find("CreateNewWarningText").GetComponent<TMP_Text>();

        CreateNewTopicButton = GameObject.Find("CreateTopicButton").GetComponent<Button>();
        CreateNewTopicButton.onClick.AddListener(onClickCreateTopic);

        CancelCreateButton = GameObject.Find("CancelCreateButton").GetComponent<Button>();
        CancelCreateButton.onClick.AddListener(onClickCancelCreate);
    }

    void Start()
    {
        resetCanvas();

        // render topics list
        StartCoroutine(renderTopicsList());

        // TODO: this toggle is a feature to implement
        AllowPostingToggle.enabled = false;
    }

    private void resetCanvas(){
        ExpandedViewCanvas.SetActive(false);
        CreateNewCanvas.SetActive(false);
    }

    private void onClickCreate(){
        CreateNewCanvas.SetActive(true);
        NewRegionName.text = Utils.GetCurrentRegionName();
        CreateNewWarnigText.text = "";
    }

    private void onClickCreateTopic(){
        // send request to backend
        string topicName = NewTopicName.text;
        string regionName = NewRegionName.text;
        DateTime time = DateTime.Now;
        Response res = TopicManager.instance.AddTopic(topicName, regionName);

        if (res.StatusCode == 403){
            CreateNewWarnigText.text = "Topic name already exists.";

        }else if (res.StatusCode == 200){
            Debug.LogFormat("Created new topic: {0}", topicName);
            NewTopicName.text = "";
            CreateNewWarnigText.text = "";
            CreateNewCanvas.SetActive(false);

            // refetch the topic list
            clearRenderedList();
            topicsList = null;
            StartCoroutine(renderTopicsList());

        }else{
            CreateNewWarnigText.text = "ERROR " + res.StatusCode.ToString() + ": Please try again.";
        }
    }

    private void clearRenderedList() {
        // clear all rendered topics
        foreach (Transform button in Content.GetComponent<RectTransform>()) {
            GameObject.Destroy(button.gameObject);
        }
    }

    private void onClickCancelCreate(){
        NewTopicName.text = "";
        CreateNewCanvas.SetActive(false);
    }

    private void onClickExpand(int id)
    {
        Topic topic = topicsList[id];
        currentExpandedID = id;

        // go to expanded view
        ExpandedViewCanvas.SetActive(true);

        // update content
        DetailsTopicName.text = topic.Name;
        DetailsRegionName.text = topic.Region;
        DetailsCreationDate.text = topic.CreatedOn.ToLocalTime().ToString();
        DetailsSubscriptionsCount.text = topic.SubscriptionCount.ToString();
        DetailsMessagesCount.text = topic.MessageCount.ToString();
    }

    private void onClickDelete(){
        // popup confirmation dialog box
        dialogObject = Dialog.Create(name: "DeleteActionDialog");
        Dialog.Load(
            title: "Warning",
            body: "Are you sure you want to delete this topic?",
            button1Text: "Yes",
            button2Text: "No",
            button2Action: onClickCancelDelete
        );

        // add action for Yes button
        GameObject.Find("DialogButton1").GetComponent<Button>().onClick.AddListener(()=>{
            StartCoroutine(onClickConfirmDelete());
        });
    }

    private void onClickCancelDelete(){
        Destroy(dialogObject);
    }

    private IEnumerator onClickConfirmDelete(){
        // delete topic from backend
        string topicID = topicsList[currentExpandedID].ID; // TODO: make this async and handle error
        TopicManager.instance.DeleteTopic(topicID);

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

        // delete rendered gameobject for the list item
        Destroy(GameObject.Find(topicID));
    }

    private IEnumerator getTopicsList(){
        yield return TopicManager.instance.GetUserTopics((topics)=>{topicsList = topics;});
    }

    private IEnumerator renderTopicsList()
    {
        // get the topics from backend
        yield return UIManager.instance.ShowLoadingOnCondition(
            tryAction: ()=>{ StartCoroutine(getTopicsList()); },
            loadingCondition: ()=>{ return topicsList == null; },
            errorCondition: ()=>{ return false; /* TODO */},
            timeoutSeconds: 30.0f
        );
        
        // render the topic list
        for (int i = 0; i < topicsList.Count; i++){
            createListItem(i);
        }

        // update text display size according to user preference
        UIManager.UpdateTextSize();
    }

    private void createListItem(int id)
    {
        Topic topic = topicsList[id];

        GameObject listItem = (GameObject) Instantiate(ListItemPrefab);
        listItem.name = topic.ID;

        // set parent
        listItem.GetComponent<RectTransform>().SetParent(Content.GetComponent<RectTransform>());
        listItem.transform.localScale = Vector2.one; // reset prefab scale after set parent to scroll rect

        // update content
        GameObject topic_label = GameObject.Find("Topic");
        topic_label.name = topic.ID;
        topic_label.GetComponent<TMP_Text>().text = topic.Name;
        GameObject messageCount = GameObject.Find("MessageCount");
        messageCount.name = topic.ID + "_messageCount";
        messageCount.GetComponent<TMP_Text>().text = topic.MessageCount.ToString();
        GameObject subscriptionCount = GameObject.Find("SubscriptionCount");
        subscriptionCount.name = topic.ID + "_subscriptionCount";
        subscriptionCount.GetComponent<TMP_Text>().text = topic.SubscriptionCount.ToString();

        Button expandButton = listItem.GetComponent<Button>();
        expandButton.onClick.AddListener(()=>{onClickExpand(id);});
    }

}
